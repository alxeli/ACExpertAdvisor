using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Color = System.Drawing.Color;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NQuotes;
using NamedPipeWrapper;

namespace ACExpertServer
{
    /// <summary>
    ///   Core logic for the ACExpert "Expert Advisor"
    ///
    ///   This EA is currently a proof of concept that sends and recieves 
    ///   information to and from the client application using a named pipe. 
    ///
    ///   Messages recieved from the client app are processed in the 
    ///   server.ClientMessage event handler.  
    /// </summary>
    public class ACExpert : MqlApi
    {
        const int MAGICMA = 20050610; //magic number to identify trade origin
        int MovingPeriod = 2; //moving average variable
        int MovingShift = 6; //moving average variable
        bool isTradingEnabled = true;

        NamedPipeServer<string> server;// = new NamedPipeServer<string>("Demo123");

        /// <summary>
        ///     Set event handler for recieving client messages
        /// </summary>
        public ACExpert()
        {
            //server.ClientMessage += delegate (NamedPipeConnection<string, string> conn, string message)
            //{
            //    InterpretClientMessage(conn, message);
            //};

            //server.Start();
        }

        /// <summary>
        ///     All logic for interpreting commands from the client application
        /// </summary>
        void InterpretClientMessage(NamedPipeConnection<string, string> conn, string message)
        {
            switch (message)
            {
                case "!c_start":
                    {
                        isTradingEnabled = true;
                    }
                    break;
                case "!c_stop":
                    {
                        isTradingEnabled = false;
                    }
                    break;
                default:
                    {
                        SendMessage("ACExpertGUI " + conn.Id + " says: " + message);
                    }
                    break;
            }
        }

        /// <summary>
        ///     sends a message to the ACExpert client
        /// </summary>
        void SendMessage(string text)
        {
            server._connections[0].PushMessage(text);
        }

        #region NQuotes Functions

        int CalculateCurrentOrders()
        {
            int buys = 0, sells = 0;

            for (int i = 0; i < OrdersTotal() && OrderSelect(i, SELECT_BY_POS, MODE_TRADES); i++)
            {
                if (OrderSymbol() == Symbol() && OrderMagicNumber() == MAGICMA)
                {
                    if (OrderType() == OP_BUY) buys++;
                    if (OrderType() == OP_SELL) sells++;
                }
            }

            //---- return orders volume
            return (buys > 0) ? buys : -sells;
        }

        //Calculate optimal lot size
        double LotsOptimized()
        {
            double MaximumRisk = 0.02;
            double DecreaseFactor = 3;

            // history orders total
            int orders = OrdersHistoryTotal();

            // number of losses orders without a break
            int losses = 0;

            // select lot size
            double lot = NormalizeDouble(AccountFreeMargin() * MaximumRisk / 1000.0, 1);

            // calcuulate number of losses orders without a break
            if (DecreaseFactor > 0)
            {
                bool isDone = false;
                for (int i = orders - 1; i >= 0 && !isDone; i--)
                {
                    if (!OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
                    {
                        Print("Error in history!");
                        isDone = true;
                    }
                    if (OrderSymbol() != Symbol() || OrderType() > OP_SELL)
                    {
                        continue;
                    }
                    if (OrderProfit() > 0)
                    {
                        isDone = true;
                    }
                    else if (OrderProfit() < 0)
                    {
                        losses++;
                    }
                }
                if (losses > 1) lot = NormalizeDouble(lot - lot * losses / DecreaseFactor, 1);
            }

            // return lot size
            if (lot < 0.1)
            {
                lot = 0.1;
            }
            return (lot);
        }

        //Check for open order conditions                 
        void CheckForOpen(string symbol)
        {
            // go trading only for first tiks of new bar
            if (Volume[0] <= 1)
            {
                // get Moving Average 
                double ma = iMA(symbol, 0, MovingPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 0);

                // sell conditions
                if (Open[1] > ma && Close[1] < ma)
                {
                    OrderSend(Symbol(), OP_SELL, LotsOptimized(), Bid, 3, 0, 0, "", MAGICMA, DateTime.MinValue, Color.Red);
                    SendMessage("Sell: " + Bid.ToString());
                }

                // buy conditions
                else if (Open[1] < ma && Close[1] > ma)
                {
                    OrderSend(Symbol(), OP_BUY, LotsOptimized(), Ask, 3, 0, 0, "", MAGICMA, DateTime.MinValue, Color.Blue);
                    SendMessage("Buy: " + Ask.ToString());
                }
            }
        }

        //Check for close order conditions          
        void CheckForClose(string symbol)
        {
            // go trading only for first tiks of new bar
            if (Volume[0] > 1) return;

            // get Moving Average 
            double ma = iMA(symbol, 0, MovingPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 0);

            bool isDone = false;

            for (int i = 0; i < OrdersTotal() && !isDone; i++)
            {
                if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
                {
                    isDone = true;
                }
                else if (OrderMagicNumber() != MAGICMA || OrderSymbol() != Symbol())
                {
                    continue;
                }

                // check order type 
                if (OrderType() == OP_BUY)
                {
                    if (Open[1] > ma && Close[1] < ma)
                    {
                        OrderClose(OrderTicket(), OrderLots(), Bid, 3, Color.White);
                    }
                }
                else if (OrderType() == OP_SELL)
                {
                    if (Open[1] < ma && Close[1] > ma)
                    {
                        OrderClose(OrderTicket(), OrderLots(), Ask, 3, Color.White);
                    }
                }
            }
        }

        //this function gets called in a loop while trading is active
        public override int start()
        {
            // check for history and trading
            if (Bars < 100 || !IsTradeAllowed() || !isTradingEnabled)
            {

                return 0;
            }

            // calculate open orders by current symbol
            string symbol = Symbol();

            if (CalculateCurrentOrders() == 0)
            {
                CheckForOpen(symbol);
            }
            else
            {
                CheckForClose(symbol);
            }

            return 0;
        }
        public override int init()
        {
            server = new NamedPipeServer<string>("Demo123");

            server.ClientMessage += delegate (NamedPipeConnection<string, string> conn, string message)
            {
                InterpretClientMessage(conn, message);
            };

            server.Start();
            Thread.Sleep(1000);
            
            SendMessage("!s_start");
            return base.init();
        }
        public override int deinit()
        {
            SendMessage("!s_stop");

            Thread.Sleep(1000);
            server.Stop();

            return base.deinit();
        }

        #endregion
    }
}
