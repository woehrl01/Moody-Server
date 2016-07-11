using System;

namespace MoodServer
{
    public interface IDbManager
    {
        int GetIdByName(string loc);
        string GetLocations();
        string MakeBarChartScript(string loc, DateTime date);
        string MakePeriodChartScript(string loc, DateTime datea, DateTime dateb, string type);
        string MakePieChartScript(string loc, DateTime date);
        string MakePieChartScriptPeriod(string loc, DateTime datea, DateTime dateb);
        string MakeSelectScript();
        void SaveMood(int mood, int location);
    }
}