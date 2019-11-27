﻿using System;
using System.Collections.Generic;
using System.Text;

/*
 * MathS.Sin() - sine of an expression
 * MathS.Cos() - cosine of an expression
 * 
 * MathS.Var() - creating an instance of variable
 * MathS.Num() - creating an instance of number (but in most cases you can use actual numbers, for example `Var("x") + 4` is ok)
 */

namespace MathSharp
{
    public class MathS
    {
        public delegate Entity OneArg(Entity a);
        public delegate Entity TwoArg(Entity a, Entity n);
        public delegate VariableEntity VarFunc(string v);
        public delegate NumberEntity NumFunc(double v);

        public static OneArg Sin = Sinf.Hang;
        public static OneArg Cos = Cosf.Hang;
        public static TwoArg Log = Logf.Hang;
        public static OneArg Ln => v => Logf.Hang(v, e);
        public static VarFunc Var = v => new VariableEntity(v);
        public static NumFunc Num = v => new NumberEntity(v);
        public static NumberEntity e = Num(2.718281828459045235);
    }
}