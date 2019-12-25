using Catel.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class ListPartD : BaseProjectPart
    {

        public int Value
        {
            get { return GetValue<int>(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly PropertyData ValueProperty = RegisterProperty(nameof(Value), typeof(int), 0);
        //public int Value { get; set; }
    }
}
