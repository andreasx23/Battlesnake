﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class Ruleset
    {
        public string Name { get; set; } = "Default";
        public string Version { get; set; } = "Default";
        public RulesetSettings Settings { get; set; } = new();
    }
}
