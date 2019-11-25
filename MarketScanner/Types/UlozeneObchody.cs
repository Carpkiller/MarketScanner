using System;
using System.Collections.Generic;

namespace MarketScanner.Types
{
    [Serializable]
    public class UlozeneObchody
    {
        public List<BackTest> ListObchodov { get; set; }
        public DateTime aktualnyDatum { get; set; }
    }
}
