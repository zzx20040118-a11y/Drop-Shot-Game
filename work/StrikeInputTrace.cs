using System;
using System.Collections.Generic;
using DropShot.Judgement;
class StrikeInputTrace {
 static List<ChartNote> Notes() => new List<ChartNote> { new ChartNote { type = "Normal", lane = 1, time = 1.024f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 2.005f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 2.987f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 3.968f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 4.949f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 5.931f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 6.912f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Long", lane = 0, time = 7.893f, duration = 0.981f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 9.383f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 9.856f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 9.875f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 10.367f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 10.859f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 11.105f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 11.595f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 11.842f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 12.334f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 12.826f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 13.318f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 13.389f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Strike", lane = -1, time = 15f, duration = 0f, targetTime = 16.235f },
new ChartNote { type = "Normal", lane = 0, time = 16.725f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 17.707f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 18.688f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 19.669f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 20.651f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 21.632f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 22.613f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Long", lane = 0, time = 23.595f, duration = 0.981f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 25.557f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 26.539f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 27.52f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 28.501f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Strike", lane = -1, time = 30.5f, duration = 0f, targetTime = 31.936f },
new ChartNote { type = "Normal", lane = 1, time = 32.427f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 33.408f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 34.389f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 35.371f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 36.352f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 37.333f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 38.315f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Long", lane = 0, time = 39.296f, duration = 0.981f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 41.259f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 42.24f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 43.221f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 44.203f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Strike", lane = -1, time = 46.5f, duration = 0f, targetTime = 47.637f },
new ChartNote { type = "Normal", lane = 1, time = 48.128f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 49.109f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 50.091f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 51.072f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 52.053f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 53.035f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 54.016f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Long", lane = 0, time = 54.997f, duration = 0.981f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 55.979f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 0, time = 56.96f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Normal", lane = 1, time = 57.941f, duration = 0f, targetTime = 0f },
new ChartNote { type = "Strike", lane = -1, time = 58f, duration = 0f, targetTime = 59.904f } };
 static void Run(string name, double down) {
  var core = new JudgeCore(Notes(), new JudgeSettings { perfectWindow=.08f, goodWindow=.15f, missWindow=.2f, strikePressWindow=.15f });
  int started=0;core.StrikeAutoStarted+=i=>Console.WriteLine(name+": shown "+i);
  core.StrikeChargeStarted+=i=>{started++; Console.WriteLine(name+": charge started "+i);};
  core.Judged+=e=>{if(e.kind==NoteKind.Strike)Console.WriteLine(name+": "+e.result);};
  core.Press(JudgeCore.StrikeLane,down);core.Tick(15.05);core.Release(JudgeCore.StrikeLane,16.235);core.Tick(16.5);
  Console.WriteLine(name+": started count = "+started);
  if(started != (down>=15 ? 1 : 0)) throw new Exception("Unexpected input result");
 }
 static void Main(){Run("after prompt",15.1);Run("same frame",15);Run("held before prompt",14.9);}
}