using System;
using BDB = Sandbox.Definitions.MyBlueprintDefinitionBase;
using IT = Sandbox.Definitions.MyBlueprintDefinitionBase.Item;
using System.Text;
using VRage.Game;

namespace Scripts.Specials.Trader {
    public class TradeOptions { //Example  Ingot/Nickel 3 10 100 10000
        public double price;
        public int step;
        public double amount;
        public double tax;
        public bool isFixed;
        public double charge;
        public double[] steps;
        public double[] prices;
        public MyDefinitionId What;
        public MyDefinitionId For;
        public MyDefinitionId Dependency;

        public bool isBuy;

        public TradeOptions(MyDefinitionId What, MyDefinitionId For, MyDefinitionId Dependency, bool isFixed, double[] steps, double[] prices, double tax) {
            this.What = What;
            this.For = For;
            this.Dependency = Dependency;
            this.isFixed = isFixed;     

            this.steps = steps;
            this.prices = prices;
            this.tax = tax;
            price = 0;
            isBuy = What == Dependency;
        }

        public double GetAmount ()
        {
            return amount;
        }

        public MyDefinitionId GetWhat()
        {
            return What;
        }

        public MyDefinitionId GetFor()
        {
            return For;
        }

        public MyDefinitionId GetDependency()
        {
            return Dependency;
        }

        public void refreshAmount(double what, double forr) {
            setAmount(what, forr);
            step = -1;
            for (int i=0; i<steps.Length; i++) {
                if (amount <= steps[i]) { step = i;  break; }
            }

            if (step == -1) { step = prices.Length-1; }

            price = prices[step];
        }

        internal void setAmount(double what, double forr)
        {
            this.amount = What == Dependency ? what : forr;
        }


        public double buyRatio => price;

        public double getBuyRatio (int step) {
            return prices[step];
        }

        public double leftToBuy {
            get {
                if (step == 0) {
                    return amount;
                } else {
                    return amount-steps[step - 1];
                }
            }
        }
        public double leftToSell {
            get {
                if (step + 1 < steps.Length) {
                    return steps[step] - amount;
                } else {
                    return Double.MaxValue;
                }
            }
        }

        public override string ToString() { return "TradeOptions { " + buyRatio + " step=" + step + " LTB:" + leftToBuy+ " }"; }

        public bool decreaseStep() {
            if (step > 0) {
                step--;
                price = prices[step];
                return true;
            } else {
                return false;
            }
        }
        
        public bool increaseStep() {
            if (step+1 < prices.Length) {
                step++;
                price = prices[step];
                return true;
            } else {
                if (steps.Length == 0)
                {
                    return true;
                }
                return false;
            }
        }

        
    }
}