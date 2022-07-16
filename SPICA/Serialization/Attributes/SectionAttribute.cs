﻿using System;

namespace SPICA.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    class SectionAttribute : Attribute
    {
        public uint SectionId;

        public int Padding;

        public SectionAttribute(uint SectionId)
        {
            this.SectionId = SectionId;
        }
    }
}
