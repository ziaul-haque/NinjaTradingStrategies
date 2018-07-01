#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class AdvancedMeanReversion : Strategy
	{
		private int NetPosition = 0;
		private double StopLossPrice = 0.0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "AdvancedMeanReversion";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				Low_return_state					= 0;
				High_return_state					= 0;
			}
			else if (State == State.Configure)
			{
				AddDataSeries("ES 12-16", Data.BarsPeriodType.Minute, 1, Data.MarketDataType.Last);
				// AddChartIndicator(MACD(Fast, Slow, Smooth));
				AddChartIndicator(Bollinger(2, 20));
				AddChartIndicator(Bollinger(4,20));
				
				ChartIndicators[0].Plots[0].Brush = Brushes.DarkRed;
				ChartIndicators[0].Plots[1].Brush = Brushes.Violet;
				ChartIndicators[0].Plots[2].Brush = Brushes.DarkBlue;
				ChartIndicators[1].Plots[0].Brush = Brushes.Violet;
				ChartIndicators[1].Plots[1].Brush = Brushes.DarkRed;
			}
		}

		protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice, 
			int quantity, int filled, double averageFillPrice, 
			Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
			
		}

		protected override void OnPositionUpdate(Cbi.Position position, double averagePrice, 
			int quantity, Cbi.MarketPosition marketPosition)
		{
			
		}

		private void LogVariables(string message)
		{
			string logVariables = string.Format("{2}, low= {0}, high = {1}", Low_return_state, High_return_state, message);
			LogMessage(logVariables);
		}
		private void LogMessage(string message)
			
		{
			Log(Time[0].ToString() + ", NetPosition =  " + NetPosition + ", " + message, LogLevel.Information);
		}
		protected override void OnBarUpdate()
		{
			if (Positions[0].MarketPosition == MarketPosition.Flat){
				// change the market state to originial condition
				High_return_state = 0;
				Low_return_state = 0;
				NetPosition = 0;
			}
			// Find the market state at each minute and based upon the current state, reacts  
			if (NetPosition > 0){
				if (Bollinger(2, 20).Lower[0] >= Low[0] && Low_return_state==4 && High_return_state == 0)
				{
						StopLossPrice = Instrument.MasterInstrument.RoundToTickSize(Bollinger(4, 20).Lower[0]);
						EnterLong(2,"L_R");
						NetPosition = NetPosition + 2;
						Low_return_state = 0;
						High_return_state = 1;
						ExitLongStopMarket(2,StopLossPrice);
						
						StopLossPrice = 0.0;
						LogVariables("Position Rebuild Entry Long");
						
				}
				else if (Bollinger(2, 20).Middle[0] <= High[0]&&Low_return_state == 3 && High_return_state==0)
				{
					ExitLong(NetPosition);
					NetPosition = NetPosition - NetPosition;
					Low_return_state=0;
					LogVariables("Exit low Long  position");
				}
				else if(Bollinger(2,20).Middle[0]<=High[0]  && Low_return_state==0 && High_return_state==1)
				{
					ExitLong(NetPosition);
					NetPosition = NetPosition - NetPosition;
					High_return_state=0;
					LogVariables("Exit high Long position");
					
				}
				else if(Bollinger(2,20).Lower[0]>=Low[0]  && Low_return_state==4 && High_return_state==0)
				{
					ExitLong(NetPosition);
					NetPosition = NetPosition - NetPosition;
					LogVariables("Exit low all long position");
				}
			}
			else if(NetPosition < 0){
				if  ( Bollinger(2, 20).Upper[0] <= High[0] && Low_return_state == 5 && High_return_state == 0)
				{
						StopLossPrice = Instrument.MasterInstrument.RoundDownToTickSize(Bollinger(4, 20).Upper[0]);
						EnterShort(2, "S_R");
						NetPosition = NetPosition - 2;
						Low_return_state = 0;       
						High_return_state =1;
						ExitShortStopMarket(2,StopLossPrice);
						StopLossPrice = 0.0;
						LogVariables("Position Rebuild Entry Short");
				}
				else if (Bollinger(2, 20).Middle[0] >= Low[0] && Low_return_state == 2 && High_return_state==0)
				{
					int qty = System.Math.Abs(NetPosition);
					ExitShort(qty);
					NetPosition = NetPosition + qty;
					Low_return_state=0;
					LogVariables("Exit low Short position");
				}
				else if(Bollinger(2,20).Upper[0]<= High[0]&&Low_return_state==5&&High_return_state==0)
				{
					int qty = System.Math.Abs(NetPosition);
					ExitShort(qty);
					NetPosition = NetPosition + qty;
					LogVariables("Exit all low Short position");
				}
				else if(Bollinger(2,20).Middle[0]>= Low[0] && Low_return_state==0 && High_return_state==1)
				{
					int qty = System.Math.Abs(NetPosition);
					ExitShort(qty);
					NetPosition = NetPosition + qty;
					High_return_state=0;
					LogVariables("Exit high Short position");
					
				}
			}
			else{
				//market high is above than BB 2 upper, reverse trend - short term opportunity - low_return
				if (Bollinger(2,20).Upper[0] <= High[0] && Low_return_state == 0 && High_return_state == 0){
					// Stoploss is defined at the BB4 - upper
					StopLossPrice = Instrument.MasterInstrument.RoundDownToTickSize(Bollinger(4,20).Upper[0]);
					EnterShort(1, "2_S");
					Low_return_state = 2;
					NetPosition = NetPosition - 1;
					ExitShortStopMarket(StopLossPrice);
					StopLossPrice = 0.0;
				}
				//market low is above than BB 2 upper, reverse trend - long term opportunity - low_return
				else if(Bollinger(2, 20).Lower[0] >= Low[0] && Low_return_state ==0 &&  High_return_state==0){
					// Stoploss is defined at the BB4 - lower
					StopLossPrice = Instrument.MasterInstrument.RoundToTickSize(Bollinger(4,20).Lower[0]);
					EnterLong(1, "3_L");
					Low_return_state = 3;
					NetPosition = NetPosition + 1;
					ExitLongStopMarket(StopLossPrice);
					StopLossPrice = 0.0;
					LogVariables("low_long");
				}
				else if(Bollinger(2, 20).Middle[0] >= Low[0] && Low_return_state ==3){
					// Stoploss is defined at the BB4 - lower
					StopLossPrice = Instrument.MasterInstrument.RoundToTickSize(Bollinger(4,20).Lower[0]);
					EnterLong(1, "4_L");
					Low_return_state = 4;
					NetPosition = NetPosition + 1;
					ExitLongStopMarket(StopLossPrice);
					StopLossPrice = 0.0;
					LogVariables("high_long");
				}
				else if (Bollinger(2,20).Middle[0] <= High[0] && Low_return_state == 2){
					// Stoploss is defined at the BB4 - upper
					StopLossPrice = Instrument.MasterInstrument.RoundDownToTickSize(Bollinger(4,20).Upper[0]);
					EnterShort(1, "5_S");
					Low_return_state = 5;
					NetPosition = NetPosition - 1;
					ExitShortStopMarket(StopLossPrice);
					StopLossPrice = 0.0;
					LogVariables("high_short");
				}	
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Low_return_state", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public int Low_return_state
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="High_return_state", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public int High_return_state
		{ get; set; }
		#endregion

	}
}
