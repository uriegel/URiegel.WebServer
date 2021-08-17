using System;

namespace UwebServer
{
    public class Route
    {
        public Method Method {get; set;}
        public string Path { get; set; }
        public Action Process { get; set; } 
    }
}