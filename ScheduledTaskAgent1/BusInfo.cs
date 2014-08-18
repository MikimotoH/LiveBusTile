using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ScheduledTaskAgent1
{
    public enum BusDir
    {
        go, back,
    };

    public class BusInfo
    {
        public BusInfo(){}
        //public BusInfo(BusInfo b)
        //{
        //    this.m_Name= String.Copy(b.m_Name);
        //    this.m_Station = String.Copy(b.m_Station);
        //    this.m_Dir = b.m_Dir;
        //    this.m_TimeToArrive = b.m_TimeToArrive;
        //}
        //public static BusInfo Copy(BusInfo b)
        //{
        //    return (BusInfo)b.MemberwiseClone();
        //}
        //public BusInfo Copy() { return BusInfo.Copy(this); }
        
        
        public string m_Name;
        public string m_Station;
        public BusDir m_Dir;
        public string m_TimeToArrive;

        public override string ToString()
        {
            return "{{m_Name=\"{0}\",m_Station=\"{1}\",m_Dir={2},m_TimeToArrive=\"{3}\" }}".Fmt(m_Name, m_Station, m_Dir, m_TimeToArrive);
        }
        public override bool Equals(object obj)
        {
            var b = obj as BusInfo;
            if (b == null)
                return false;
            if (m_TimeToArrive == null )
                return m_Name.Equals(b.m_Name)
                    && m_Station.Equals(b.m_Station)
                    && m_Dir.Equals(b.m_Dir);
            else 
                return m_Name.Equals(b.m_Name) 
                    && m_Station.Equals(b.m_Station) 
                    && m_Dir.Equals(b.m_Dir) 
                    && m_TimeToArrive.Equals(b.m_TimeToArrive);
        }
        public override int GetHashCode()
        {
            if (m_TimeToArrive == null)
                return m_Name.GetHashCode()
                    ^ m_Station.GetHashCode()
                    ^ m_Dir.GetHashCode();
            else
                return m_Name.GetHashCode()
                    ^ m_Station.GetHashCode()
                    ^ m_Dir.GetHashCode()
                    ^ m_TimeToArrive.GetHashCode();
        }

        public string DirWithDestStation
        {
            get
            {
                var stats = Database.AllBuses[m_Name].GetStations(m_Dir);
                string pfx = "往：";
                if (m_Dir == BusDir.back)
                    pfx = "返：";
                if (stats.Length > 0)
                    return pfx + stats.LastElement();
                else return pfx;
            }
        }
    }



    public class BusGroup
    { 
        public string m_GroupName;
        public List<BusInfo> m_Buses;
        public BusGroup() { }
        public BusGroup(BusGroup bg)
        {
            this.m_GroupName = String.Copy(bg.m_GroupName);
            this.m_Buses = new List<BusInfo>(bg.m_Buses);
        }

        public static BusGroup Copy(BusGroup bg)
        {
            return (BusGroup)bg.MemberwiseClone();
        }
        public BusGroup Copy(){return BusGroup.Copy(this);}

        public override string ToString()
        {
            return "m_GroupName={0}, m_Buse={1}".Fmt(m_GroupName, m_Buses.DumpArray());
        }
    }

    public class BusAndDir
    {
        public string bus;
        public BusDir dir;
    }

    public class StationPair
    {
        public string[] stations_go;
        public string[] stations_back;

        public string[] GetStations(BusDir m_dir)
        {
            return m_dir == BusDir.go ? stations_go : stations_back;
        }
    }


}
