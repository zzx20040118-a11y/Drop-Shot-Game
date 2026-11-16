using System;
using System.Collections.Generic;
using DropShot.Judgement;
class RestoreLongCheck {
static int count;
static void Check(bool ok,string name){if(!ok)throw new Exception(name); Console.WriteLine("PASS "+name);count++;}
static void Main(){foreach(var settings in new[]{new JudgeSettings(),new JudgeSettings{perfectWindow=.08f,goodWindow=.15f,missWindow=.2f}}){
foreach(double time in new[]{3.0,3.3}){
var events=new List<JudgeEvent>();int ends=0;var core=new JudgeCore(new[]{new ChartNote{type="Long",lane=0,time=2,duration=1}},settings);core.Judged+=e=>events.Add(e);core.LongHoldEnded+=i=>ends++;core.Press(0,2);core.Tick(time);
Check(events.Count==2 && events[1].phase==JudgePhase.Tail && events[1].result==JudgeResult.Perfect && core.Finished && ends==1,"Hold through end auto Perfect "+time);core.Release(0,4);core.Tick(5);Check(events.Count==2&&ends==1,"No duplicate tail");}
foreach(var pair in new[]{(-.02,JudgeResult.Perfect),(-(settings.perfectWindow+settings.goodWindow)/2,JudgeResult.Good),(-settings.goodWindow-.02,JudgeResult.Miss)}){
var events=new List<JudgeEvent>();var core=new JudgeCore(new[]{new ChartNote{type="Long",lane=0,time=2,duration=1}},settings);core.Judged+=e=>events.Add(e);core.Press(0,2);core.Release(0,3+pair.Item1);Check(events.Count==2&&events[1].result==pair.Item2,"Early release "+pair.Item2);}
{var events=new List<JudgeEvent>();var core=new JudgeCore(new[]{new ChartNote{type="Normal",lane=0,time=1}},settings);core.Judged+=e=>events.Add(e);core.Press(0,1.02);Check(events.Count==1&&events[0].result==JudgeResult.Perfect,"Normal unchanged");}
foreach(bool release in new[]{true,false}){var events=new List<JudgeEvent>();var core=new JudgeCore(new[]{new ChartNote{type="Strike",lane=-1,time=4,targetTime=4.5f}},settings);core.Judged+=e=>events.Add(e);core.TryStartStrike(4);if(release)core.Release(-1,4.52);else core.Tick(5);Check(events.Count==1&&events[0].result==(release?JudgeResult.Perfect:JudgeResult.Miss),"Strike release/timeout unchanged");}
}Console.WriteLine("RESULT "+count+" passed");}}
