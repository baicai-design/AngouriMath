﻿
/* Copyright (c) 2019-2020 Angourisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;

namespace AngouriMath
{
    public abstract partial record Entity
    {
        partial record Continuous
        {
            public partial record Number : Entity
            {
                protected override Entity InnerEval() => this;
                internal override Entity InnerSimplify() => this;
            }
            public partial record Variable : Entity
            {
                // TODO: When target-typed conditional expression lands, remove the explicit conversion
                protected override Entity InnerEval() => ConstantList.TryGetValue(this, out var value) ? (Entity)value : this;
                internal override Entity InnerSimplify() => this;
            }
            public partial record Tensor : Entity
            {
                protected override Entity InnerEval() => Elementwise(e => e.Evaled);
                internal override Entity InnerSimplify() => Elementwise(e => e.InnerSimplify());
            }
            // Each function and operator processing
            public partial record Sumf
            {
                protected override Entity InnerEval() => (Augend.Evaled, Addend.Evaled) switch
                {
                    (Complex n1, Complex n2) => n1 + n2,
                    (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 + n2).Evaled),
                    (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 + n2).Evaled),
                    (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 + n2).Evaled),
                    (var n1, var n2) => New(n1, n2)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : (Augend.InnerSimplify(), Addend.InnerSimplify()) switch
                    {
                        (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 + n2).InnerSimplify()),
                        (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 + n2).InnerSimplify()),
                        (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 + n2).InnerSimplify()),
                        (var n1, Integer(0)) => n1,
                        (Integer(0), var n2) => n2,
                        (var n1, var n2) => n1 == n2 ? (2 * n1).InnerSimplify() : New(n1, n2)
                    };
            }
            public partial record Minusf
            {
                protected override Entity InnerEval() => (Subtrahend.Evaled, Minuend.Evaled) switch
                {
                    (Complex n1, Complex n2) => n1 - n2,
                    (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 - n2).Evaled),
                    (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 - n2).Evaled),
                    (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 - n2).Evaled),
                    (var n1, var n2) => New(n1, n2)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : (Subtrahend.InnerSimplify(), Minuend.InnerSimplify()) switch
                    {
                        (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 - n2).InnerSimplify()),
                        (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 - n2).InnerSimplify()),
                        (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 - n2).InnerSimplify()),
                        (var n1, Integer(0)) => n1,
                        (Integer(0), var n2) => (-n2).InnerSimplify(),
                        (var n1, var n2) => n1 == n2 ? (Entity)0 : New(n1, n2)
                    };
            }
            public partial record Mulf
            {
                protected override Entity InnerEval() => (Multiplier.Evaled, Multiplicand.Evaled) switch
                {
                    (Complex n1, Complex n2) => n1 * n2,
                    (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 * n2).Evaled),
                    (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 * n2).Evaled),
                    (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 * n2).Evaled),
                    (var n1, var n2) => New(n1, n2)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : (Multiplier.InnerSimplify(), Multiplicand.InnerSimplify()) switch
                    {
                        (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 * n2).InnerSimplify()),
                        (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 * n2).InnerSimplify()),
                        (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 * n2).InnerSimplify()),
                        (_, Integer(0)) or (Integer(0), _) => 0,
                        (var n1, Integer(1)) => n1,
                        (Integer(1), var n2) => n2,
                        (var n1, var n2) => n1 == n2 ? new Powf(n1, 2).InnerSimplify() : New(n1, n2)
                    };
            }
            public partial record Divf
            {
                protected override Entity InnerEval() => (Dividend.Evaled, Divisor.Evaled) switch
                {
                    (Complex n1, Complex n2) => n1 / n2,
                    (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 / n2).Evaled),
                    (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 / n2).Evaled),
                    (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 / n2).Evaled),
                    (var n1, var n2) => New(n1, n2)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : (Dividend.InnerSimplify(), Divisor.InnerSimplify()) switch
                    {
                        (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => (n1 / n2).InnerSimplify()),
                        (var n1, Tensor n2) => n2.Elementwise(n2 => (n1 / n2).InnerSimplify()),
                        (Tensor n1, var n2) => n1.Elementwise(n1 => (n1 / n2).InnerSimplify()),
                        (Integer(0), _) => 0,
                        (_, Integer(0)) => Real.NaN,
                        (var n1, Integer(1)) => n1.InnerSimplify(),
                        (var n1, var n2) => n1 == n2 ? (Entity)1 : New(n1, n2)
                    };
            }
            public partial record Powf
            {
                protected override Entity InnerEval() => (Base.Evaled, Exponent.Evaled) switch
                {
                    (Complex n1, Complex n2) => Number.Pow(n1, n2),
                    (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => n1.Pow(n2).Evaled),
                    (var n1, Tensor n2) => n2.Elementwise(n2 => n1.Pow(n2).Evaled),
                    (Tensor n1, var n2) => n1.Elementwise(n1 => n1.Pow(n2).Evaled),
                    (var n1, var n2) => New(n1, n2)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : (Base.InnerSimplify(), Exponent.InnerSimplify()) switch
                    {
                        (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => n1.Pow(n2).InnerSimplify()),
                        (var n1, Tensor n2) => n2.Elementwise(n2 => n1.Pow(n2).InnerSimplify()),
                        (Tensor n1, var n2) => n1.Elementwise(n1 => n1.Pow(n2).InnerSimplify()),
                    // 0^x is undefined for Re(x) <= 0
                    (Integer(1), _) => 0,
                        (var n1, Integer(-1)) => (1 / n1).InnerSimplify(),
                        (_, Integer(0)) => 1,
                        (var n1, Integer(1)) => n1.InnerSimplify(),
                        (var n1, var n2) => New(n1, n2)
                    };
            }
            public partial record Sinf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Sin(n),
                    Tensor n => n.Elementwise(n => n.Sin().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Sin().InnerSimplify()),
                        { Evaled: Complex n } when TrigonometryTableValues.PullSin(n, out var res) => res,
                        var n => New(n)
                    };
            }
            public partial record Cosf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Cos(n),
                    Tensor n => n.Elementwise(n => n.Cos().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Cos().InnerSimplify()),
                        { Evaled: Complex n } when TrigonometryTableValues.PullCos(n, out var res) => res,
                        var n => New(n)
                    };
            }
            public partial record Tanf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Tan(n),
                    Tensor n => n.Elementwise(n => n.Tan().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Tan().InnerSimplify()),
                        { Evaled: Complex n } when TrigonometryTableValues.PullTan(n, out var res) => res,
                        var n => New(n)
                    };
            }
            public partial record Cotanf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Cotan(n),
                    Tensor n => n.Elementwise(n => n.Cotan().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Cotan().InnerSimplify()),
                        { Evaled: Complex n } when TrigonometryTableValues.PullTan(n, out var res) => 1 / res,
                        var n => New(n)
                    };
            }
            public partial record Logf
            {
                protected override Entity InnerEval() => (Base.Evaled, Antilogarithm.Evaled) switch
                {
                    (Complex n1, Complex n2) => Number.Log(n1, n2),
                    (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => n1.Log(n2).Evaled),
                    (var n1, Tensor n2) => n2.Elementwise(n2 => n1.Log(n2).Evaled),
                    (Tensor n1, var n2) => n1.Elementwise(n1 => n1.Log(n2).Evaled),
                    (var n1, var n2) => New(n1, n2)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : (Base.InnerSimplify(), Antilogarithm.InnerSimplify()) switch
                    {
                        (Tensor n1, Tensor n2) => n1.Elementwise(n2, (n1, n2) => n1.Log(n2).InnerSimplify()),
                        (var n1, Tensor n2) => n2.Elementwise(n2 => n1.Log(n2).InnerSimplify()),
                        (Tensor n1, var n2) => n1.Elementwise(n1 => n1.Log(n2).InnerSimplify()),
                        (_, Integer(0)) => Real.NegativeInfinity,
                        (_, Integer(1)) => 0,
                        (var n1, var n2) => n1 == n2 ? (Entity)1 : New(n1, n2)
                    };
            }
            public partial record Arcsinf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Arcsin(n),
                    Tensor n => n.Elementwise(n => n.Arcsin().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Arcsin().InnerSimplify()),
                        var n => New(n)
                    };
            }
            public partial record Arccosf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Arccos(n),
                    Tensor n => n.Elementwise(n => n.Arccos().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Arccos().InnerSimplify()),
                        var n => New(n)
                    };
            }
            public partial record Arctanf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Arctan(n),
                    Tensor n => n.Elementwise(n => n.Arctan().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Arctan().InnerSimplify()),
                        var n => New(n)
                    };
            }
            public partial record Arccotanf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Arccotan(n),
                    Tensor n => n.Elementwise(n => n.Arccotan().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Arccotan().InnerSimplify()),
                        var n => New(n)
                    };
            }
            public partial record Factorialf
            {
                protected override Entity InnerEval() => Argument.Evaled switch
                {
                    Complex n => Number.Factorial(n),
                    Tensor n => n.Elementwise(n => n.Factorial().Evaled),
                    var n => New(n)
                };
                internal override Entity InnerSimplify() =>
                    Evaled is Number { IsExact: true } ? Evaled : Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(n => n.Factorial().InnerSimplify()),
                        Rational({ Numerator: var num, Denominator: var den }) when den.Equals(2) && (num + 1) / 2 is var en => (
                            en > 0
                            // (+n - 1/2)! = (2n-1)!/(2^(2n-1)(n-1)!)*sqrt(pi)
                            // also 2n-1 is the numerator
                            ? Rational.Create(num.Factorial(), PeterO.Numbers.EInteger.FromInt32(2).Pow(num) * (en - 1).Factorial())
                            // (-n - 1/2)! = (-4)^n*n!/(2n)!*sqrt(pi)
                            : Rational.Create(PeterO.Numbers.EInteger.FromInt32(-4).Pow(-en) * (-en).Factorial(), (2 * -en).Factorial())
                        ) * MathS.Sqrt(MathS.pi),
                        var n => New(n)
                    };
            }
            public partial record Derivativef
            {
                protected override Entity InnerEval() => (Expression.Evaled, Var.Evaled, Iterations.Evaled) switch
                {
                    (Tensor expr, var var, var iters) => expr.Elementwise(n => new Derivativef(n, var, iters).Evaled),
                    (var expr, _, Integer(0)) => expr,
                    // TODO: consider Integral for negative cases
                    (var expr, Variable var, Integer { EInteger: var asInt }) => expr.Derive(var, asInt),
                    _ => this
                };
                internal override Entity InnerSimplify() =>
                    Var.InnerSimplify() is Variable var && Iterations.InnerSimplify() is Integer { EInteger: var asInt }
                    ? asInt.IsZero
                      ? Expression.InnerSimplify()
                      : Expression.Derive(var, asInt)
                    : this;
            }
            public partial record Integralf
            {
                protected override Entity InnerEval() => (Expression.Evaled, Var.Evaled, Iterations.Evaled) switch
                {
                    (Tensor expr, var var, var iters) => expr.Elementwise(n => new Integralf(n, var, iters).Evaled),
                    (var expr, _, Integer(0)) => expr,
                    // TODO: consider Derivative for negative cases
                    (var expr, Variable var, Integer { EInteger: var asInt }) =>
                        throw new NotImplementedException("Integration is not implemented yet"),
                    _ => this
                };
                internal override Entity InnerSimplify() =>
                    Var.InnerSimplify() is Variable var && Iterations.InnerSimplify() is Integer { EInteger: var asInt }
                    ? asInt.IsZero
                      ? Expression.InnerSimplify()
                      : throw new NotImplementedException("Integration is not implemented yet")
                    : this;
            }
            public partial record Limitf
            {
                protected override Entity InnerEval() => ApproachFrom switch
                {
                    ApproachFrom.Left => throw new NotImplementedException("Limits unevaluable"), // TODO
                    ApproachFrom.BothSides => throw new NotImplementedException("Limits unevaluable"), // TODO
                    ApproachFrom.Right => throw new NotImplementedException("Limits unevaluable"), // TODO
                    _ => this,
                };
                internal override Entity InnerSimplify() =>
                    Var.InnerSimplify() switch
                    {
                        Entity.Variable x => MathS.Compute.Limit(Expression.InnerSimplify(), x, Destination.InnerSimplify(), ApproachFrom) ?? this,
                        var x => new Limitf(Expression.InnerSimplify(), x, Destination.InnerSimplify(), ApproachFrom)
                    };

            }

            public partial record Signumf
            {
                protected override Entity InnerEval()
                    => Argument.Evaled switch
                    {
                        Complex n => Complex.Signum(n),
                        Tensor n => n.Elementwise(c => c.Signum().Evaled),
                        var n => this
                    };

                // TODO: probably we can simplify it further
                internal override Entity InnerSimplify()
                    => Argument.Evaled is Number { IsExact: true } ? Argument.Evaled :
                    Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(c => c.Signum().InnerEval()),
                        var n => this
                    };
            }

            public partial record Absf
            {
                protected override Entity InnerEval()
                    => Argument.Evaled switch
                    {
                        Complex n => Complex.Abs(n),
                        Tensor n => n.Elementwise(c => c.Abs().Evaled),
                        var n => this
                    };

                // TODO: probably we can simplify it further
                internal override Entity InnerSimplify()
                    => Argument.Evaled is Number { IsExact: true } ? Argument.Evaled :
                    Argument.InnerSimplify() switch
                    {
                        Tensor n => n.Elementwise(c => c.Signum().InnerEval()),
                        var n => this
                    };
            }
        }
    }
}
