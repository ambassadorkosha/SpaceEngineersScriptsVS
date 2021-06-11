using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using System.Linq;


namespace Torpedo
{
	public sealed class Program : MyGridProgram
	{
		//------------BEGIN--------------

		static Program myScript;
		Torpedo Tor;
		int tick = 0;
		int n_Obekt = 0;
		int k = 10;


		IMyProgrammableBlock prog;
		//MyDetectedEntityInfo Target;
		MyDetectedEntityInfo[] CurrentTarget;
		List<IMyTerminalBlock> Turrets;
		List<IMyTerminalBlock> TextPanelStatus;
		IMyTextSurface c;

		public Program()
		{
			prog = Me;
			myScript = this;


			c = Me.GetSurface(0);
			c.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE; //ContentType.TEXT_AND_IMAGE;
			c.FontSize = 2;
			//c.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
			c.WriteText("", false);

			Tor = new Torpedo();

			Turrets = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyLargeConveyorTurretBase>(Turrets, (a) => (a.IsSameConstructAs(prog)));
			TP_Echo("турели", Turrets);

			TextPanelStatus = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName("TextPanelStatus", TextPanelStatus);
			TP_Echo("TextPanelStatus", TextPanelStatus);

			CurrentTarget = new MyDetectedEntityInfo[k];




			Runtime.UpdateFrequency = UpdateFrequency.Update1;
		}
		public void Main(string argument)
		{
			tick++;
			if (tick % 1 == 0)
			{
				Turel();
				TP();
			}
			if (tick % 1 == 0 && n_Obekt > 0)
			{
				Tor.Current_Target();
			}
		}
		void TP_Echo(string nameBlock, List<IMyTerminalBlock> Temps)
		{
			Echo(nameBlock + " " + (Temps.Count).ToString() + " шт.");
			c.WriteText(nameBlock + " " + (Temps.Count).ToString() + " шт." + "\n", true);
		}


		void Turel()
		{
			foreach (IMyLargeConveyorTurretBase Turret in Turrets)
			{
				if (Turret.IsFunctional)
				{
					if (Turret.HasTarget)
					{
						Obekt(Turret.GetTargetedEntity());
					}
				}
			}
		}

		void Obekt(MyDetectedEntityInfo temp)
		{
			int i_temp = 0;
			for (int i = 0; i < n_Obekt; i++)
			{
				if (temp.EntityId == CurrentTarget[i].EntityId)
				{
					break;//i = n_Obekt;

				}
				else i_temp++;
			}
			if (i_temp == n_Obekt || n_Obekt == 0)
			{
				CurrentTarget[n_Obekt] = temp;
				n_Obekt++;
			}
		}

		void TP()
		{
			string textStatus = "";
			for (int i = 0; i < n_Obekt; i++)
			{
				textStatus += (i + 1).ToString() + "Id цели " + (CurrentTarget[i].EntityId).ToString() + "\n";
			}

			foreach (IMyTextPanel temp in TextPanelStatus)
			{
				if (temp.IsFunctional)
				{
					//TempTextPanel.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;  // выводит по центру
					temp.WriteText(textStatus, false);
				}
			}


		}

		public class Torpedo
		{
			private List<IMyTerminalBlock>[] Torpedo_Gyro;
			private List<IMyTerminalBlock>[] Torpedo_Thruster;
			private List<IMyTerminalBlock>[] Torpedo_Merge;
			private IMyCockpit Cockpit;
			private string Prefix = "Torpedo";
			private int otstikovka = 240;
			private int[,] TorpedoStatus;
			private long[,] CurrentTarget_Status;
			private Vector3D MyPos, MyVelocity, MyPrevPos, gravityVektor;


			public Torpedo()
			{
				List<IMyTerminalBlock> n_Torpedo = new List<IMyTerminalBlock>();
				myScript.GridTerminalSystem.SearchBlocksOfName(Prefix + "_Gyro", n_Torpedo);
				myScript.TP_Echo("Колличество торпед", n_Torpedo);

				Torpedo_Gyro = new List<IMyTerminalBlock>[n_Torpedo.Count + 1];
				Torpedo_Thruster = new List<IMyTerminalBlock>[n_Torpedo.Count + 1];
				Torpedo_Merge = new List<IMyTerminalBlock>[n_Torpedo.Count + 1];
				TorpedoStatus = new int[n_Torpedo.Count + 1, 3];                        // 0= жива или нет торпеда, 1=вылетила или нет торпеда, 2=тик с момента запуска трастера
				for (int i = 1; i < n_Torpedo.Count + 1; i++)
				{
					myScript.GridTerminalSystem.SearchBlocksOfName(Prefix + "_Gyro" + "_" + i, Torpedo_Gyro[i]);
					myScript.GridTerminalSystem.SearchBlocksOfName(Prefix + "_Thruster" + "_" + i, Torpedo_Thruster[1]);
					myScript.GridTerminalSystem.SearchBlocksOfName(Prefix + "_Merge" + "_" + i, Torpedo_Merge[i]);
					myScript.Echo("Торпеда " + i.ToString() + " собранна");
					myScript.c.WriteText("Торпеда " + i.ToString() + " собранна" + "\n", true);
					TorpedoStatus[i, 0] = 1;
				}

				CurrentTarget_Status = new long[myScript.k, n_Torpedo.Count + 1];
				Cockpit = myScript.GridTerminalSystem.GetBlockWithName("Cockpit") as IMyCockpit;
				gravityVektor = Cockpit.GetNaturalGravity();
			}

			public void Current_Target()
			{
				int temp_n_tor = 0;

				if (myScript.CurrentTarget[0].EntityId != 0)
				{
					myScript.c.WriteText("CurrentTarget[0] = " + (myScript.CurrentTarget[0].EntityId).ToString(), false);
					myScript.c.WriteText("   2635477 = " + CurrentTarget_Status[0, 0].ToString(), true);
					for (int i = 0; i < myScript.k; i++)
					{
						if (myScript.CurrentTarget[0].EntityId == CurrentTarget_Status[i, 0])
						{
							CurrentTarget_Status[i, 1] = 1;////////
							temp_n_tor = (int)CurrentTarget_Status[i, 1];
							myScript.Echo("CurrentTarget_Status = " + CurrentTarget_Status[i, 1].ToString());
							myScript.c.WriteText("CurrentTarget_Status = " + CurrentTarget_Status[i, 1].ToString(), false);
							break;
						}
						else if (i == myScript.k - 1) CurrentTarget_Status[myScript.n_Obekt, 0] = myScript.CurrentTarget[0].EntityId;
					}

					if (TorpedoStatus[temp_n_tor, 0] == 1)
					{
						myScript.c.WriteText("111111 ", false);
						if (TorpedoStatus[temp_n_tor, 1] == 2)
						{
							Update(temp_n_tor);
						}
						else Ignition(temp_n_tor);
					}

				}
			}

			private void Ignition(int n_tor)        //запуск торпеды
			{
				myScript.c.WriteText("222222 ", false);
				myScript.c.WriteText("  n_tor " + n_tor.ToString(), true);
				myScript.c.WriteText("  TorpedoStatus[n_tor, 1] " + n_tor.ToString(), true);
				if (TorpedoStatus[n_tor, 1] == 0)
				{

					foreach (IMyThrust Thruster in Torpedo_Thruster[1])
					{
						if (Thruster.IsFunctional)
						{
							Thruster.ApplyAction("OnOff_On");
							Thruster.ThrustOverridePercentage = 100f;
						}
					}
					myScript.c.WriteText("333333 ", false);
					foreach (IMyMechanicalConnectionBlock Merge in Torpedo_Merge[n_tor])
					{
						if (Merge.IsFunctional) Merge.ApplyAction("OnOff_Off");
					}
					TorpedoStatus[n_tor, 1] = 1;
					TorpedoStatus[n_tor, 2] = myScript.tick + otstikovka;

				}
				else if (TorpedoStatus[n_tor, 1] == 1)
				{
					if (TorpedoStatus[n_tor, 2] < myScript.tick)
					{
						TorpedoStatus[n_tor, 1] = 2;
					}
				}
			}


			private void Update(int n_tor)
			{
				foreach (IMyGyro Gyro in Torpedo_Gyro[n_tor])
				{
					if (Gyro.IsFunctional)
					{
						MyPos = Gyro.GetPosition();
						MyVelocity = (MyPos - MyPrevPos) * 60;
						MyPrevPos = MyPos;

						Vector3D InterceptVector = Vector3D.Normalize(FindInterceptVector(MyPos, MyVelocity.Length(), myScript.CurrentTarget[0].Position, myScript.CurrentTarget[0].Velocity));
						Vector3D vectorDown = Vector3D.Normalize(MyVelocity + gravityVektor);
						SetGyroOverride(true, (InterceptVector).Cross(Gyro.WorldMatrix.Forward), (vectorDown).Cross(Gyro.WorldMatrix.Forward), n_tor);





						break;
					}
				}


			}

			private Vector3D FindInterceptVector(Vector3D shotOrigin, double shotSpeed, Vector3D targetOrigin, Vector3D targetVel)
			{
				Vector3D dirToTarget = Vector3D.Normalize(targetOrigin - shotOrigin);
				Vector3D targetVelOrth = Vector3D.Dot(targetVel, dirToTarget) * dirToTarget;
				Vector3D targetVelTang = targetVel - targetVelOrth;
				Vector3D shotVelTang = targetVelTang;
				double shotVelSpeed = shotVelTang.Length();

				if (shotVelSpeed > shotSpeed)
				{
					return Vector3D.Normalize(targetVel) * shotSpeed;
				}
				else
				{
					double shotSpeedOrth = Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
					Vector3D shotVelOrth = dirToTarget * shotSpeedOrth;
					return shotVelOrth + shotVelTang;
				}
			}

			public void SetGyroOverride(bool OverrideOnOff, Vector3 settings, Vector3 vectorDown, int n_tor, float Power = 1f)
			{
				if (vectorDown.Length() == 0) vectorDown = settings;
				foreach (IMyGyro gyro in Torpedo_Gyro[n_tor])
				{
					if (gyro.IsFunctional)
					{
						gyro.GyroOverride = OverrideOnOff;
						gyro.Pitch = (float)((settings.Dot(gyro.WorldMatrix.Right))) * Power;
						gyro.Yaw = (float)((settings.Dot(gyro.WorldMatrix.Up))) * Power;
						gyro.Roll = (float)(vectorDown.Dot(gyro.WorldMatrix.Backward)) * Power * 2;
					}
				}
			}

		}
		//------------END--------------
	}
}