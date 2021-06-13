using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Scripts.Base;

namespace Scripts.Specials.LCDScripts
{
    public class AutoLCDBuffer
    {
        public class SortClass
        {
            public string Where;
            public string Name;
            public Dictionary<string, double> items;
            public List<string> exceptions;
            public bool isGroup;
            public bool IsSeparate;
            public bool isZeroHidden;
            public bool IsOnSameGrid;
            public bool isProgBarCubeVisible;
            public ShowType SType = ShowType.Default;
        }

        public enum ShowType
        {
            Default,
            OnlyPercentage,
            OnlyExactVolume,
            NoExactVolume,
            OnlyProgressBar
        }
        
        public enum AutoLCDInfoType
        {
            PowerStored,
            PowerUsing,
            Time,
            Volume
        }
        private Regex locationRegex = new Regex("\\{.*?\\}");
        public Regex nameRegex = new Regex("\\[.*?\\]");

        private const string TagGroup = "g:";
        private const string TagHideZero = "z";
        private const string TagSeparate = "s";
        private const string TagSameGrid = "g";
        private const string TagBarCubeOn = "c";
        private const string TagOnlyPercentage = "x";
        private const string TagNoExactVolume = "p";
        private const string TagOnlyExactVolume = "v";
        private const string TagOnlyProgressBar = "bar";

        public readonly Dictionary<int, SortClass> InventorySort = new Dictionary<int,SortClass>();
        public readonly Dictionary<int, SortClass> MissingSort = new Dictionary<int,SortClass>();
        public readonly Dictionary<int, SortClass> CargoSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> CargoAllSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> PowerSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> PowerStoredSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> PowerUsedSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> PowerTimeSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> ChargeSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> DamageSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> DockedSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> BlockCountSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> ProdCountSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> EnabledCountSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> WorkingSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> PropBoolSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> DetailsSort = new Dictionary<int, SortClass>();
        /*
         public readonly Dictionary<int, SortClass> AmountSort = new Dictionary<int, SortClass>();
         public readonly Dictionary<int, SortClass> OxygenSort = new Dictionary<int, SortClass>();
        public readonly Dictionary<int, SortClass> TanksSort = new Dictionary<int, SortClass>();
        */
        public readonly Dictionary<int, string> EchoSort = new Dictionary<int, string>();
        public readonly Dictionary<int, string> CenterSort = new Dictionary<int, string>();
        public readonly Dictionary<int, string> RightSort = new Dictionary<int, string>();
        public readonly Dictionary<int, Pair<string,bool>> HScrollSort = new Dictionary<int, Pair<string,bool>>();
        public readonly Dictionary<int, string> CustomDataSort = new Dictionary<int, string>();
        public readonly Dictionary<int, string> TextLCDSort = new Dictionary<int, string>();
        public readonly Dictionary<int, Pair<int,bool>> TimeSort = new Dictionary<int, Pair<int,bool>>();
        public readonly Dictionary<int, Pair<int,bool>> DateSort = new Dictionary<int, Pair<int,bool>>();
        public readonly Dictionary<int, Pair<Pair<int,bool>,string>> DateTimeSort = new Dictionary<int, Pair<Pair<int,bool>,string>>();
        public readonly Dictionary<int, Pair<DateTime,int>> CountDownSort = new Dictionary<int, Pair<DateTime,int>>();
        public readonly Dictionary<int, Pair<int,string>> PosSort = new Dictionary<int, Pair<int,string>>();
        public readonly Dictionary<int, bool> AltitudeSort = new Dictionary<int, bool>();
        public readonly Dictionary<int, Pair<int,int>> SpeedSort = new Dictionary<int, Pair<int,int>>();
        public readonly Dictionary<int, int> AccelSort = new Dictionary<int, int>();
         /*
          public readonly Dictionary<int, SortClass> ShipMassCustomText = new Dictionary<int, SortClass>();
         public readonly Dictionary<int, SortClass> MassCustomText = new Dictionary<int, SortClass>();
         public readonly Dictionary<int, SortClass> OccupiedCustomText = new Dictionary<int, SortClass>();
         public readonly Dictionary<int, SortClass> DistanceCustomText = new Dictionary<int, SortClass>();
         */
        public readonly Dictionary<int, string> CoresSort = new Dictionary<int, string>();
        public readonly Dictionary<int, SortClass> GarageSort = new Dictionary<int, SortClass>(); 
         
        private string RegexParse(Regex InRegexType, string text, out bool isGroup)
        {
            var whereToSearch = "";
            isGroup = false;
            
            var match = InRegexType.Match(text);
            if (match.Success)
            {
                var matched = match.Captures[0].ToString();
                whereToSearch = matched.Substring(1, matched.Length - 2);
                if (whereToSearch.ToLower().Contains(TagGroup))
                {
                    isGroup = true;
                    whereToSearch = whereToSearch.Substring(2);
                }
            }
            return whereToSearch;
        }
        public string RegexParse(Regex InRegexType, string text)
        {
            var name = "";
            var match = InRegexType.Match(text);
            
            if (match.Success)
            {
                var matched = match.Captures[0].ToString();
                name = matched.Substring(1, matched.Length - 2); 
            }
            return name;
        }
        public void ParseCargoCommand(string text, int line, bool All = false)
        {
            var strings = text.Split(' ');
            var Type = ShowType.Default;
            bool isGroup;
            var isMerge = true;
            var isOneGrid = false;
            var isBarCubeOn = false;

            foreach (var str in strings)
            {
                if (str.Contains("Cargo") || str.Contains("CargoAll"))
                {
                    var _str = str.ToLower().Replace("cargo", "").Replace("all","");
                    if (_str.Contains(TagBarCubeOn)) isBarCubeOn = true;
                    if (_str.Contains(TagSeparate)) isMerge = false;
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                    
                    if (_str.Contains(TagNoExactVolume)) Type = ShowType.NoExactVolume;
                    if (_str.Contains(TagOnlyPercentage)) Type = ShowType.OnlyPercentage;
                    if (_str.Contains(TagOnlyExactVolume)) Type = ShowType.OnlyExactVolume;
                    if (_str.Contains(TagOnlyProgressBar)) Type = ShowType.OnlyProgressBar;
                }
            }
            
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            var name = RegexParse(nameRegex,text);
            
            if(All) CargoAllSort.Add(line, new SortClass{Where = whereToSearch, Name = name,isGroup = isGroup, IsSeparate = isMerge, IsOnSameGrid = isOneGrid, SType = Type, isProgBarCubeVisible = isBarCubeOn});
            else CargoSort.Add(line, new SortClass{Where = whereToSearch, Name = name,isGroup = isGroup, IsSeparate = isMerge, IsOnSameGrid = isOneGrid, SType = Type, isProgBarCubeVisible = isBarCubeOn});
            
        }
        public void ParseInventoryCommand(string text, int line, bool missing = false)
        {
            var buffer = text.Split(' ');
            var _TBuffer = new Dictionary<string, double>();
            var itemsToRemove = new List<string>();
            var Type = ShowType.Default;
            bool isGroup;
            var hideZero = false;
            var isOneGrid = false;
            var isBarCubeOn = false;
            
            foreach (var str in buffer)
            {
                if (str.Contains("Inventory") || str.Contains("Missing"))
                {
                    var _str = str.ToLower().Replace("inventory", "").Replace("missing", "");
                    if (_str.Contains(TagHideZero)) hideZero = true;
                    if (_str.Contains(TagGroup)) isOneGrid = true;
                    if (_str.Contains(TagBarCubeOn)) isBarCubeOn = true;
                    if (_str.Contains(TagOnlyPercentage)) hideZero = true;
                    if (_str.Contains(TagOnlyExactVolume)) Type = ShowType.OnlyExactVolume;

                    continue;
                }
                double limit = 0;

                if (str.StartsWith("+") && str.Length > 1)
                {
                    var tStr = str.Substring(1);
                    string item;
                    if (tStr.Contains(":"))
                    {
                        var spl = tStr.Split(':');
                        item = spl[0];
                        double.TryParse(spl[1], out limit);
                    }
                    else item = tStr;

                    _TBuffer.Add(item, limit);
                    continue;
                }

                if (str.StartsWith("-") && str.Length > 1)
                {
                    itemsToRemove.Add(str.Substring(1));
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            var name = RegexParse(nameRegex,text);
            if(missing) MissingSort.Add(line, new SortClass{Where = whereToSearch, items = _TBuffer,exceptions = itemsToRemove,Name = name, isGroup = isGroup, IsOnSameGrid = isOneGrid, isProgBarCubeVisible = isBarCubeOn, SType = Type});
            else        InventorySort.Add(line, new SortClass{Where = whereToSearch, items = _TBuffer,exceptions = itemsToRemove,Name = name, isGroup = isGroup, isZeroHidden = hideZero, IsOnSameGrid = isOneGrid, isProgBarCubeVisible = isBarCubeOn, SType = Type});
        }
        public void ParsePowerCommand(string text, int line, bool isStored = false)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var Type = ShowType.Default;
            var isOneGrid = false;
            var isBarCubeOn = false;

            foreach (var str in strings)
            {
                if (str.Contains("Power"))
                {
                    var _str = str.ToLower().Replace("power", "");
                    if (_str.Contains(TagBarCubeOn)) isBarCubeOn = true;
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;

                    if (_str.Contains(TagNoExactVolume)) Type = ShowType.NoExactVolume;
                    if (_str.Contains(TagOnlyPercentage)) Type = ShowType.OnlyPercentage;
                    if (_str.Contains(TagOnlyExactVolume)) Type = ShowType.OnlyExactVolume;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            var name = RegexParse(nameRegex,text);
            if(!isStored) PowerSort.Add(line,new SortClass{Where = whereToSearch,Name = name,isGroup = isGroup,IsOnSameGrid = isOneGrid,isProgBarCubeVisible = isBarCubeOn,SType = Type});
            else PowerStoredSort.Add(line,new SortClass{Where = whereToSearch,Name = name,isGroup = isGroup,IsOnSameGrid = isOneGrid,isProgBarCubeVisible = isBarCubeOn,SType = Type});
        }
        public void ParsePowerUsedCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var Type = ShowType.Default;
            var isOneGrid = false;
            var isBarCubeOn = false;
            var isTop = false;
            var NumberOfTops = 0;
            
            foreach (var str in strings)
            {
                if (str.Contains("PowerUsed"))
                {
                    var _str = str.ToLower().Replace("powerused", "");
                    if (_str.Contains(TagBarCubeOn)) isBarCubeOn = true;
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                    if (_str.Contains("top")) isTop = true;
                    
                    if (_str.Contains(TagNoExactVolume)) Type = ShowType.NoExactVolume;
                    if (_str.Contains(TagOnlyPercentage)) Type = ShowType.OnlyPercentage;
                    if (_str.Contains(TagOnlyExactVolume)) Type = ShowType.OnlyExactVolume;
                    continue;
                }
                if (isTop) int.TryParse(str, out NumberOfTops);
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            var name = RegexParse(nameRegex,text);
            PowerUsedSort.Add(line,new SortClass{Where = whereToSearch,Name =  name,isGroup = isGroup,IsOnSameGrid = isOneGrid,isProgBarCubeVisible = isBarCubeOn,SType = Type,items = new Dictionary<string, double>{{"Top",NumberOfTops}}, IsSeparate = isTop});
        }
        public void ParsePowerTimeCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var isOneGrid = false;
            bool isGroup;
            var Type = ShowType.Default;
            
            foreach (var str in strings)
            {
                if (str.Contains("PowerTime"))
                {
                    var _str = str.ToLower().Replace("powertime", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                        
                    if (_str.Contains(TagNoExactVolume)) Type = ShowType.NoExactVolume;
                    if (_str.Contains(TagOnlyPercentage)) Type = ShowType.OnlyPercentage;
                    if (_str.Contains(TagOnlyExactVolume)) Type = ShowType.OnlyExactVolume;
                    if (_str.Contains(TagOnlyProgressBar)) Type = ShowType.OnlyProgressBar;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            PowerTimeSort.Add(line,new SortClass{Where = whereToSearch, isGroup = isGroup, IsOnSameGrid = isOneGrid, SType = Type});
        }
        public void ParseChargeCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var Type = ShowType.Default;
            var isOneGrid = false;
            var isBarCubeOn = false;
            var isSeparate = false;
            var isTime = false;

            foreach (var str in strings)
            {
                if (str.Contains("Charge"))
                {
                    var _str = str.ToLower().Replace("charge", "");
                    if (_str.Contains("time"))
                    {
                        _str = _str.Replace("time", "");
                        isTime = true;
                    }
                    if (_str.Contains(TagBarCubeOn)) isBarCubeOn = true;
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                    if (_str.Contains(TagSeparate)) isSeparate = true;
                    
                    if (_str.Contains(TagNoExactVolume)) Type = ShowType.NoExactVolume;
                    if (_str.Contains(TagOnlyPercentage)) Type = ShowType.OnlyPercentage;
                    if (_str.Contains(TagOnlyExactVolume)) Type = ShowType.OnlyExactVolume;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            ChargeSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid,isProgBarCubeVisible = isBarCubeOn,SType = Type, IsSeparate = isSeparate, isZeroHidden = isTime});
        }
        public void ParseDamageCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("Damage"))
                {
                    var _str = str.ToLower().Replace("damage", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text, out isGroup);
            DamageSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
        public void ParseDockedCommand(string text, int line)
        {
            bool isGroup;
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            
            DockedSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup});
        }
        public void ParseBlockCountCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("BlockCount"))
                {
                    var _str = str.ToLower().Replace("blockcount", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            BlockCountSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
        public void ParseProdCountCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("ProdCount"))
                {
                    var _str = str.ToLower().Replace("prodcount", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            ProdCountSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
        public void ParseEnableCountCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("EnableCount"))
                {
                    var _str = str.ToLower().Replace("enablecount", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            EnabledCountSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
        public void ParseWorkingCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("Working"))
                {
                    var _str = str.ToLower().Replace("working", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            WorkingSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
        public void ParsePropBoolCommand(string text, int line)
        {
            var strings = text.Split(' ');

            bool isGroup;
            var isOneGrid = false;

            var words = new List<string>();

            foreach (var str in strings)
            {
                if (str.Contains("PropBool"))
                {
                    var _str = str.ToLower().Replace("propbool", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            var name = RegexParse(nameRegex,text);
            PropBoolSort.Add(line,new SortClass{Where = whereToSearch, isGroup = isGroup, IsOnSameGrid = isOneGrid, Name = name, exceptions = words});
        }
        public void ParseDetailsCommand(string text, int line) //todo
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("Details"))
                {
                    var _str = str.ToLower().Replace("details", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;

                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            DetailsSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
        public void ParseEchoCommand(string text, int line, bool right = false)
        {
            var length = right ? 6 : 4;
            text = text.Substring(length);
            if (text.StartsWith(" ")) text = text.Substring(1);
            if(!right) EchoSort.Add(line, text);
            else RightSort.Add(line, text);
        }
        public void ParseCenterCommand(string text, int line)
        {
            CenterSort.Add(line, text.Substring(6));
        }
        public void ParseHScrollCommand(string text, int line)
        {
            string _text;
            var Right = false;
            if (text.StartsWith("HScrollR"))
            {
                Right = true;
                _text = text.Substring(8);
            }
            else _text = text.Substring(7);
            
            HScrollSort.Add(line, new Pair<string, bool>(_text, Right));
        }
        public void ParseCustomDataCommand(string text, int line)
        {
            text = text.Substring(10);
            if (text.StartsWith(" ")) text = text.Substring(1);
            CustomDataSort.Add(line,text);
        }
        public void ParseTextLCDCommand(string text, int line)
        {
            text = text.Substring(7);
            if (text.StartsWith(" ")) text = text.Substring(1);
            TextLCDSort.Add(line,text);
        }
        public void ParseTimeCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var isCentered = false;
            var Offset = 0;

            foreach (var str in strings)
            {
                if (str.Contains("Time"))
                {
                    var _str = str.ToLower().Replace("time", "");
                    if (_str.Contains("c"))
                    {
                        _str = _str.Replace("c", "");
                        isCentered = true;
                    }

                    int.TryParse(_str, out Offset);
                }
            }
            TimeSort.Add(line,new Pair<int, bool>(Offset,isCentered));
        }
        public void ParseDateCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var isCentered = false;
            var Offset = 0;

            foreach (var str in strings)
            {
                if (str.Contains("Date"))
                {
                    var _str = str.ToLower().Replace("date", "");
                    if (_str.Contains("c"))
                    {
                        _str = _str.Replace("c", "");
                        isCentered = true;
                    }
                    int.TryParse(_str, out Offset);
                }
            }
            DateSort.Add(line,new Pair<int, bool>(Offset,isCentered));
        }
        public void ParseDateTimeCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var isCentered = false;
            var format = "";
            var Offset = 0;

            foreach (var str in strings)
            {
                if (str.Contains("DateTime"))
                {
                    var _str = str.ToLower().Replace("datetime", "");
                    if (_str.Contains("c"))
                    {
                        _str = _str.Replace("c", "");
                        isCentered = true;
                    }
                    int.TryParse(_str, out Offset);
                    continue;
                }
                format += str + " ";
            }
            DateTimeSort.Add(line,new Pair<Pair<int, bool>, string>(new Pair<int, bool>(Offset,isCentered), format));
        }
        public void ParseCountDownCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var AlignmentType = 0;
            int Day = 1, Month = 1, Year = 1, Hour = 0, Min = 0;

            foreach (var str in strings)
            {
                if (str.Contains("Countdown"))
                {
                    var _str = str.ToLower().Replace("countdown", "");
                    if (_str.Contains("c")) AlignmentType = 1;
                    if (_str.Contains("r")) AlignmentType = 2;
                    continue;
                }

                if (str.Contains(":"))
                {
                    var _Time = str.Split(':');
                    if (_Time.Length >= 2)
                    {
                        int.TryParse(_Time[0], out Hour);
                        int.TryParse(_Time[1], out Min);
                    }
                    continue;
                }

                if (str.Contains("."))
                {
                    var _Date = str.Split('.');
                    if (_Date.Length >= 3)
                    {
                        int.TryParse(_Date[0], out Day);
                        int.TryParse(_Date[1], out Month);
                        int.TryParse(_Date[2], out Year);
                    }
                }
            }
            CountDownSort.Add(line, new Pair<DateTime, int>(new DateTime(Year, Month, Day, Hour, Min, 0), AlignmentType));
        }
        public void ParsePosCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var Type = 0;
            bool isGroup;
            foreach (var str in strings)
            {
                if (str.Contains("Pos"))
                {
                    var _str = str.ToLower().Replace("pos", "");
                    switch (_str)
                    {
                        case "xyz": Type = 1; break;
                        case "gps": Type = 2; break;
                        default: Type = 0; break;
                    }
                }
            }
            var whereToSearch = RegexParse(locationRegex,text,out isGroup);
            PosSort.Add(line,new Pair<int, string>(Type,whereToSearch));
        }
        public void ParseAltitudeCommand(string text, int line)
        {
            AltitudeSort.Add(line,text.Contains("Sea"));
        }
        public void ParseSpeedCommand(string text, int line)
        {
            var strings = text.Split(' ');
            var Type = 0;
            var Limit = 0;
            foreach (var str in strings)
            {
                if (str.Contains("Speed"))
                {
                    var _str = str.ToLower().Replace("speed", "");
                    switch (_str)
                    {
                        case "kmh": Type = 1; break;
                        case "mph": Type = 2; break;
                        default: Type = 0; break;
                    }
                    continue;
                }
                int.TryParse(str, out Limit);
            }
            SpeedSort.Add(line,new Pair<int, int>(Type,Limit));
        }
        public void ParseAccelCommand(string text, int line)
        {
            int Limit;
            int.TryParse(text.ToLower().Replace("accel", ""), out Limit);
            AccelSort.Add(line,Limit);
        }
        public void ParseCoresCommand(string text, int line)
        {
            var Text = text.Replace("Cores", "");
            var name = RegexParse(nameRegex, Text);
            CoresSort.Add(line,name);
        }
        public void ParseGaragesCommand(string text, int line)
        {
            var strings = text.Split(' ');
            bool isGroup;
            var isOneGrid = false;

            foreach (var str in strings)
            {
                if (str.Contains("Garages"))
                {
                    var _str = str.ToLower().Replace("garages", "");
                    if (_str.Contains(TagSameGrid)) isOneGrid = true;
                }
            }
            var whereToSearch = RegexParse(locationRegex,text, out isGroup);
            GarageSort.Add(line,new SortClass{Where = whereToSearch,isGroup = isGroup,IsOnSameGrid = isOneGrid});
        }
    }
}
