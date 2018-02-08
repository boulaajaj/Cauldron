﻿using Mono.Cecil.Cil;
using System;

namespace Cauldron.Interception.Cecilator.Coders
{
    public sealed partial class BooleanExpressionResultCoder : IBitwiseOperators<BooleanExpressionResultCoder, BooleanExpressionCoder>
    {
        public BooleanExpressionResultCoder And(Func<BooleanExpressionCoder, BooleanExpressionResultCoder> other)
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            other(otherCoder);
            x.instructions.Append(x.processor.Create(OpCodes.And));
            return this;
        }

        public BooleanExpressionResultCoder And(Field field)
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            x.instructions.Append(x.AddParameter(x.processor, Builder.Current.TypeSystem.Boolean, field).Instructions);
            x.instructions.Append(x.processor.Create(OpCodes.And));
            return this;
        }

        public BooleanExpressionResultCoder And(LocalVariable variable)
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            x.instructions.Append(x.AddParameter(x.processor, Builder.Current.TypeSystem.Boolean, variable).Instructions);
            x.instructions.Append(x.processor.Create(OpCodes.And));
            return this;
        }

        public BooleanExpressionResultCoder Invert()
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            this.coder.instructions.Append(this.coder.processor.Create(OpCodes.Ldc_I4_0));
            this.coder.instructions.Append(this.coder.processor.Create(OpCodes.Ceq));
            return this;
        }

        public BooleanExpressionResultCoder Or(Field field)
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            x.instructions.Append(x.AddParameter(x.processor, Builder.Current.TypeSystem.Boolean, field).Instructions);
            x.instructions.Append(x.processor.Create(OpCodes.Or));
            return this;
        }

        public BooleanExpressionResultCoder Or(LocalVariable variable)
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            x.instructions.Append(x.AddParameter(x.processor, Builder.Current.TypeSystem.Boolean, variable).Instructions);
            x.instructions.Append(x.processor.Create(OpCodes.Or));
            return this;
        }

        public BooleanExpressionResultCoder Or(Func<BooleanExpressionCoder, BooleanExpressionResultCoder> other)
        {
            var x = this.coder;
            var otherCoder = new BooleanExpressionCoder(this.coder);
            this.RemoveJump();
            other(otherCoder);
            x.instructions.Append(x.processor.Create(OpCodes.Or));
            return this;
        }
    }
}