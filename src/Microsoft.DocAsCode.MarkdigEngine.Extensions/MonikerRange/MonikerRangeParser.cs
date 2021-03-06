// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;

    public class MonikerRangeParser : BlockParser
    {
        private const string StartString = "moniker";
        private const string EndString = "moniker-end";
        private const char Colon = ':';
        public MonikerRangeParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            if (ExtensionsHelper.IsEscaped(slice))
            {
                return BlockState.None;
            }

            var column = processor.Column;
            var sourcePosition = processor.Start;
            var colonCount = 0;

            var c = slice.CurrentChar;
            while (c == Colon)
            {
                c = slice.NextChar();
                colonCount++;
            }

            if (colonCount < 3) return BlockState.None;

            SkipSapces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "moniker", false))
            {
                return BlockState.None;
            }

            SkipSapces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "range=\"", false))
            {
                return BlockState.None;
            }

            var range = StringBuilderCache.Local();
            c = slice.CurrentChar;

            while (c != '"')
            {
                range.Append(c);
                c = slice.NextChar();
            }

            if (c != '"')
            {
                return BlockState.None;
            }

            c = slice.NextChar();
            while (c.IsSpace())
            {
                c = slice.NextChar();
            }

            if (!c.IsZero())
            {
                Logger.LogWarning($"MonikerRange have some invalid chars in the starting.");
            }

            processor.NewBlocks.Push(new MonikerRangeBlock(this)
            {
                MonikerRange = range.ToString(),
                ColonCount = colonCount,
                Column = column,
                Span = new SourceSpan(sourcePosition, slice.End),
            });

            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.IsBlankLine)
            {
                return BlockState.Continue;
            }

            var slice = processor.Line;
            var monikerRange = (MonikerRangeBlock)block;

            SkipSapces(ref slice);

            if(!ExtensionsHelper.MatchStart(ref slice, new string(':', monikerRange.ColonCount)))
            {
                return BlockState.Continue;
            }

            SkipSapces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "moniker-end", false))
            {
                return BlockState.Continue;
            }

            var c = SkipSapces(ref slice);

            if (!c.IsZero())
            {
                Logger.LogWarning($"MonikerRange have some invalid chars in the ending.");
            }

            block.UpdateSpanEnd(slice.End);

            return BlockState.BreakDiscard;
        }

        public char SkipSapces(ref StringSlice slice)
        {
            var c = slice.CurrentChar;

            while (c.IsSpaceOrTab())
            {
                c = slice.NextChar();
            }

            return c;
        }
    }
}
