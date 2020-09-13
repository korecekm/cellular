using Cellular.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cellular.Controller
{
    class ConfigurationReader
    {
        TextReader textReader;
        StringBuilder warnings;
        List<string> lines;
        bool[] ignoreLines;
        // Macros map, where keys are macro names and values are the substitution strings:
        Dictionary<string, string> macros;

        public ConfigurationReader(TextReader reader)
        {
            textReader = reader;
        }

        private String UnexpectedEOF =>  String.Format("ERROR: Unexpected end of file at line {0}.", iterateLine + 1);

        /// <summary>
        /// Loads the whole config file via this ConfigReaders TextReader.
        /// </summary>
        /// <param name="colorList">
        /// Initialized List of CellColors to which the colors specified in
        /// said config file are added.
        /// </param>
        public LoadResult Load(ref List<CellColor> colorList)
        {
            lines = new List<string>();
            // Both IOException and OutOfMemory will be thrown if they occur.
            String line = textReader.ReadLine();
            while (line != null)
            {
                lines.Add(line);
                line = textReader.ReadLine();
            }
            textReader.Close();
            ignoreLines = new bool[lines.Count];

            warnings = new StringBuilder();

            try
            {
                macros = new Dictionary<string, string>();
                LoadMacros();

                rawColors = new List<CellColorRaw>();
                colorNameIdxs = new Dictionary<string, int>();
                while (NextChar())
                {
                    ReadColor();
                }

                if (rawColors.Count < 2)
                    return LoadResult.ErrorResult("ERROR: At least 2 colors need to be defined for an automaton.");

                // Turn raw colors into final CellColors:
                // First, initiate all the CellColor objects (colorList is expected to be empty, otherwise this code is incorrect).
                for (int i = 0; i < rawColors.Count; ++i)
                {
                    var rawColor = rawColors[i];
                    colorList.Add(new CellColor
                    {
                        Index = i,
                        Name = rawColor.name,
                        Color = rawColor.color,
                        RelevantCells = rawColor.relevantCells,
                        Rules = new List<Rule>()
                    });
                }
                // Now use the initiated object references to initiate Rules:
                for (int i = 0; i < rawColors.Count; ++i)
                {
                    foreach (var rule in rawColors[i].rules)
                    {
                        if (!colorNameIdxs.ContainsKey(rule.resultColor))
                            return LoadResult.ErrorResult(String.Format(
                                "ERROR: Undefined color '{0}' specified in a rule definition for cell color '{1}'.", rule.resultColor, rawColors[i].name));
                        var newRule = new Rule(colorList[colorNameIdxs[rule.resultColor]]);
                        foreach (var condition in rule.conditions)
                        {
                            List<CellColor> validColors = new List<CellColor>();
                            foreach (string validColor in condition.validColors)
                            {
                                if (!colorNameIdxs.ContainsKey(validColor))
                                    return LoadResult.ErrorResult(String.Format(
                                        "ERROR: Undefined color '{0}' used in a condition for cell color '{1}'.", validColor, rawColors[i].name));
                                validColors.Add(colorList[colorNameIdxs[validColor]]);
                            }
                            if (condition.targetCount != null)
                                newRule.Conditions.Add(new CountCondition(condition.squares, validColors, (int)condition.targetCount, (CountCondition.Ordering)condition.order));
                            else
                                newRule.Conditions.Add(new KindCondition(condition.squares, validColors));
                        }
                        colorList[i].Rules.Add(newRule);
                    }
                }
            } catch (LoadException e)
            {
                return LoadResult.ErrorResult(e.Message);
            }

            if (warnings.Length == 0) return LoadResult.OkResult;
            else
            {
                warnings.Insert(0, "WARNING: Your configuration was successfully loaded with these warnings, indicating potential mistakes:" + Environment.NewLine);
                return new LoadResult
                {
                    result = LoadResult.Result.WARN,
                    message = warnings.ToString()
                };
            }
        }

        private char currentChar;
        private int iterateLine = -1;
        private bool wordStart = true;
        private CharEnumerator lineEnumerator;
        private char postMacroChar;
        private CharEnumerator macroEnumerator;
        private StringBuilder macroNameBuilder = new StringBuilder();

        /// <summary>
        /// Helps iterate through the characters of the config file after macros have been
        /// preprocessed. Simply gives chars as if there were no macros, just expanded
        /// configuration text.
        /// </summary>
        private bool NextChar()
        {
            if (macroEnumerator != null)
            {
                if (macroEnumerator.MoveNext())
                {
                    currentChar = macroEnumerator.Current;
                }
                else
                {
                    currentChar = postMacroChar;
                    macroEnumerator = null;
                    wordStart = true;
                }
                return true;
            }
            if (lineEnumerator == null)
            {
                ++iterateLine;
                if (iterateLine >= lines.Count) return false;
                while (ignoreLines[iterateLine])
                {
                    ++iterateLine;
                    if (iterateLine >= lines.Count) return false;
                }
                lineEnumerator = lines[iterateLine].GetEnumerator();
                wordStart = true;
            }
            if (lineEnumerator.MoveNext())
            {
                char current = lineEnumerator.Current;
                if (!CharIsLegal(current))
                    throw new LoadException(String.Format(
                            "ERROR: Illegal character ({0}) in line {1}", current, iterateLine + 1
                        ));
                if (current.Equals('/'))
                {
                    if (!lineEnumerator.MoveNext() || !lineEnumerator.Current.Equals('/'))
                    {
                        throw new LoadException(String.Format(
                            "ERROR, line {0}: Single slash (/) is invalid", iterateLine + 1
                        ));
                    }
                    else
                    {
                        lineEnumerator = null;
                        return NextChar();
                    }
                }
                if (wordStart)
                {
                    if (Char.IsUpper(current))
                    {
                        // Encountered a macro.
                        macroNameBuilder.Clear();
                        macroNameBuilder.Append(current);
                        while (true)
                        {
                            if (!lineEnumerator.MoveNext())
                            {
                                postMacroChar = ' ';
                                break;
                            }
                            else if (!Char.IsLetterOrDigit(lineEnumerator.Current) && !lineEnumerator.Current.Equals('_'))
                            {
                                postMacroChar = lineEnumerator.Current;
                                break;
                            }
                            else
                            {
                                macroNameBuilder.Append(lineEnumerator.Current);
                            }
                        }
                        string macroName = macroNameBuilder.ToString();
                        string macro;
                        if (!macros.TryGetValue(macroName, out macro))
                        {
                            throw new LoadException(String.Format(
                                "ERROR: Unknown macro name '{0}', line {1}.", macroName, iterateLine + 1
                            ));
                        }
                        macroEnumerator = macro.GetEnumerator();
                        return NextChar();
                    }
                    wordStart = false;
                }
                if (!Char.IsLetterOrDigit(current) && !current.Equals('#'))
                    wordStart = true;
                currentChar = current;
                return true;
            }
            lineEnumerator = null;
            currentChar = ' ';
            return true;
        }

        private void SkipWhiteSpace()
        {
            while (Char.IsWhiteSpace(currentChar) && NextChar()) { }
        }

        private Dictionary<string, int> colorNameIdxs;
        private List<CellColorRaw> rawColors;

        private void ReadColor()
        {
            CellColorRaw rawColor = ReadName();
            if (rawColor == null) return;

            if (colorNameIdxs.ContainsKey(rawColor.name))
                throw new LoadException(String.Format(
                    "ERROR, line {0}: Duplicit cell collor name '{1}'", iterateLine + 1, rawColor.name));
            colorNameIdxs[rawColor.name] = rawColors.Count;
            rawColor.color = ReadColorDefinition();
            ReadColorRules(ref rawColor);

            rawColors.Add(rawColor);
        }

        /// <summary>
        /// Reads a continuous sequence of characters and initializes a CellColorRaw object with
        /// name being this sequence.
        /// If unsuccessful, returns null and writes into readColorError
        /// </summary>
        private CellColorRaw ReadName()
        {
            StringBuilder nameBuilder = new StringBuilder();
            SkipWhiteSpace();

            // Read sequence until whitespace:
            while (!Char.IsWhiteSpace(currentChar))
            {
                if (!Char.IsLetterOrDigit(currentChar) && !currentChar.Equals('_'))
                {
                    throw new LoadException(
                        String.Format("ERROR, line {0}: Character '{1}' is invalid in a cell color name.", iterateLine + 1, currentChar));
                }
                nameBuilder.Append(currentChar);
                if (!NextChar())
                {
                    throw new LoadException(UnexpectedEOF);
                }
            }
            if (nameBuilder.Length == 0)
                return null;
            // Name must start with a lowercase letter:
            if (!Char.IsLower(nameBuilder[0]))
                throw new LoadException(String.Format("ERROR, line {0}: Color name must start with a lowercase letter.", iterateLine + 1));
            CellColorRaw result = new CellColorRaw();
            result.name = nameBuilder.ToString();
            return result;
        }
        private Color ReadColorDefinition()
        {
            SkipWhiteSpace();
            if (currentChar.Equals('#'))
                return ReadHexColorDefinition();
            if (currentChar.Equals('('))
                return ReadRGBColorDefinition();
            throw new LoadException(String.Format("ERROR, line {0}: Hexadecimal or (R, G, B) color specification expected.", iterateLine + 1));
        }

        /// <summary>
        /// Reads the definition while '#' is currentChar
        /// </summary>
        private Color ReadHexColorDefinition()
        {
            StringBuilder definitionBuilder = new StringBuilder();
            NextChar();
            while (!Char.IsWhiteSpace(currentChar) && !currentChar.Equals('{'))
            {
                if (!Char.IsLetterOrDigit(currentChar))
                    throw new LoadException(String.Format("ERROR: line {0}: Invalid hex-format color specification.", iterateLine + 1));
                definitionBuilder.Append(currentChar);
                if (!NextChar())
                    throw new LoadException(UnexpectedEOF);
            }
            if (definitionBuilder.Length != 6)
                throw new LoadException(String.Format("ERROR: line {0}: Invalid hex-format color specification.", iterateLine + 1));

            byte[] hexConversion = new byte[6];
            for (int i = 0; i < 6; ++i)
            {
                if (Char.IsDigit(definitionBuilder[i]))
                    hexConversion[i] = (byte)((byte)definitionBuilder[i] - (byte)'0');
                else
                {
                    // Letter between 'a' and 'f'
                    int order = Char.IsLower(definitionBuilder[i])
                        ? (int)definitionBuilder[i] - (int)'a'
                        : (int)definitionBuilder[i] - (int)'A';
                    if (order > 5)
                        throw new LoadException(
                            String.Format("ERROR: line {0}: Invalid hex-format color specification '#{1}'.", iterateLine + 1, definitionBuilder.ToString()));
                    hexConversion[i] = (byte)(order + 10);
                }
            }

            return Color.FromArgb(hexConversion[0] * 16 + hexConversion[1],
                                  hexConversion[2] * 16 + hexConversion[3],
                                  hexConversion[4] * 16 + hexConversion[5]);
        }

        /// <summary>
        /// Reads the definition while '(' is currentChar
        /// </summary>
        private Color ReadRGBColorDefinition()
        {
            StringBuilder definitionBuilder = new StringBuilder();
            byte[] rgb = new byte[3];
            NextChar();

            int rgbIdx = 0;
            while (true)
            {
                if (rgbIdx > 2)
                    throw new LoadException(String.Format("ERROR, line {0}: Invalid RGB color definition.", iterateLine + 1));

                SkipWhiteSpace();
                if (Char.IsDigit(currentChar))
                    definitionBuilder.Append(currentChar);
                else if (currentChar.Equals(',') || currentChar.Equals(')'))
                {
                    byte value;
                    if (definitionBuilder.Length == 0 || !byte.TryParse(definitionBuilder.ToString(), out value))
                        throw new LoadException(String.Format("ERROR, line {0}: Invalid RGB color definition.", iterateLine + 1));
                    rgb[rgbIdx] = value;
                    ++rgbIdx;
                    definitionBuilder.Clear();
                    if (currentChar.Equals(')'))
                        break;
                }
                else
                    throw new LoadException(
                        String.Format("ERROR: line {0}: Character '{1}' is invalid in an RGB color definition.", iterateLine + 1, currentChar));

                if (!NextChar())
                    throw new LoadException(UnexpectedEOF);
            }
            if (rgbIdx != 3)
                throw new LoadException(String.Format("ERROR, line {0}: Invalid RGB color definition.", iterateLine + 1));

            NextChar();
            return Color.FromArgb(rgb[0], rgb[1], rgb[2]);
        }

        private void ReadColorRules(ref CellColorRaw color)
        {
            SkipWhiteSpace();
            if (!currentChar.Equals('{'))
                throw new LoadException(String.Format("ERROR, line {0}: Unexpected character '{1}'.", iterateLine + 1, currentChar));
            color.rules = new List<RuleRaw>();
            color.relevantCells = new List<Location>();
            NextChar();
            while (true)
            {
                SkipWhiteSpace();
                if (currentChar.Equals('}'))
                    break;
                var rule = ReadRawRule(ref color.relevantCells);
                if (rule == null) return;
                color.rules.Add(rule);
            }
        }

        private RuleRaw ReadRawRule(ref List<Location> relevantCells)
        {
            if (!currentChar.Equals('['))
                throw new LoadException(String.Format("ERROR, line {0}: Unexpected character '{1}'." + Environment.NewLine +
                    "Square brackets expected containing a rule definition.", iterateLine + 1, currentChar));
            RuleRaw rule = new RuleRaw();
            rule.conditions = new List<ConditionRaw>();

            NextChar();
            while (true)
            {
                SkipWhiteSpace();
                if (currentChar.Equals(']'))
                    break;
                var condition = ReadRawCondition(ref relevantCells);
                if (condition == null)
                    return null;
                rule.conditions.Add(condition);
                SkipWhiteSpace();
                if (!currentChar.Equals(',') && !currentChar.Equals(']'))
                    throw new LoadException(String.Format("ERROR, line {0}:" + Environment.NewLine +
                        "'{1}' is not a valid condition separator, ',' expected (or ']' if all conditions were specified).", iterateLine + 1, currentChar));
                if (currentChar.Equals(','))
                    NextChar();
            }
            NextChar();
            if (rule.conditions.Count == 0)
                throw new LoadException(String.Format("ERROR, line {0}: No conditions specified for a rule.", iterateLine + 1));

            // result color:
            SkipWhiteSpace();
            char prev = currentChar;
            NextChar();
            if (!prev.Equals('-') || !currentChar.Equals('>'))
                throw new LoadException(String.Format("ERROR, line {0}: '->' and the resulting color expected here.", iterateLine + 1));
            NextChar();
            SkipWhiteSpace();

            StringBuilder resultColorBuilder = new StringBuilder();
            while (Char.IsLetterOrDigit(currentChar) || currentChar.Equals('_'))
            {
                resultColorBuilder.Append(currentChar);
                if (!NextChar())
                    throw new LoadException(UnexpectedEOF);
            }
            rule.resultColor = resultColorBuilder.ToString();
            return rule;
        }

        private ConditionRaw ReadRawCondition(ref List<Location> relevantCells)
        {
            SkipWhiteSpace();
            if (currentChar.Equals('('))
                return ReadRawKindCondition(ref relevantCells);
            else
            {
                char[] expect = { 'c', 'o', 'u', 'n', 't', '(' };
                foreach (char c in expect)
                {
                    if (!currentChar.Equals(c))
                        throw new LoadException(String.Format("ERROR in line {0}: Unexpected character '{1}' in condition definition." + Environment.NewLine +
                            "Did you mean to write a count condition ( starting with 'count(' )?", iterateLine + 1, currentChar));
                    if (!NextChar())
                        throw new LoadException(UnexpectedEOF);
                }
                return ReadRawCountCondition(ref relevantCells);
            }
        }

        /// <summary>
        /// 'count(' should have been just read at the time this method is called
        /// ( currentChar being the one after that )
        /// </summary>
        private ConditionRaw ReadRawCountCondition(ref List<Location> relevantCells)
        {
            ConditionRaw condition = new ConditionRaw();
            condition.squares = new List<int>();
            condition.validColors = new List<string>();

            // Relevant cells:
            while (true)
            {
                SkipWhiteSpace();
                if (currentChar.Equals(')'))
                    break;
                int index = ReadLocationIntoRelevantCells(ref relevantCells);
                condition.squares.Add(index);
                SkipWhiteSpace();
                if (!currentChar.Equals(',') && !currentChar.Equals(')'))
                    throw new LoadException(String.Format("ERROR, line {0}:" + Environment.NewLine +
                        "'{1}' is not a valid location separator, ',' expected (or ')' if all relevant cells were specified).", iterateLine + 1, currentChar));
                if (currentChar.Equals(','))
                    NextChar();
            }
            if (condition.squares.Count == 0)
                throw new LoadException(String.Format("ERROR, line {0}: No square locations specified in count condition.", iterateLine + 1));

            NextChar();
            SkipWhiteSpace();
            if (!currentChar.Equals('('))
                throw new LoadException(
                    String.Format("ERROR: line {0}: Unexpected character '{1}'. Cell color names should be specified here in another parentheses.",
                    iterateLine + 1, currentChar));
            NextChar();

            StringBuilder sb = new StringBuilder();
            // Relevant cell colors:
            while (true)
            {
                SkipWhiteSpace();
                if (currentChar.Equals(')'))
                    break;
                sb.Clear();
                while (Char.IsLetterOrDigit(currentChar) || currentChar.Equals('_'))
                {
                    sb.Append(currentChar);
                    if (!NextChar())
                        throw new LoadException(UnexpectedEOF);
                }
                if (sb.Length == 0)
                    throw new LoadException(String.Format("ERROR: line {0}: Invalid relevant colors definition in count condition.", iterateLine + 1));
                condition.validColors.Add(sb.ToString());
                SkipWhiteSpace();
                if (!currentChar.Equals(',') && !currentChar.Equals(')'))
                    throw new LoadException(String.Format("ERROR, line {0}:" + Environment.NewLine +
                        "'{1}' is not a valid cell color name separator, ',' expected (or ')' if all relevant colors were specified)."
                        , iterateLine + 1, currentChar));
                if (currentChar.Equals(','))
                    NextChar();
            }
            if (condition.validColors.Count == 0)
                throw new LoadException(String.Format("ERROR, line {0}: No valid cell colors specified in count condition.", iterateLine + 1));

            // Finally, read operator and target count:
            NextChar();
            SkipWhiteSpace();
            switch (currentChar)
            {
                case '<':
                    condition.order = CountCondition.Ordering.LESS;
                    break;
                case '>':
                    condition.order = CountCondition.Ordering.GREATER;
                    break;
                case '=':
                    condition.order = CountCondition.Ordering.EQUAL;
                    break;
                default:
                    throw new LoadException(String.Format("ERROR, line {0}: Unexpected character '{1}'." + Environment.NewLine +
                        "'<', '>', or '=' expected here as a valid count condition operator.", iterateLine + 1, currentChar));
            }
            NextChar();
            SkipWhiteSpace();
            sb.Clear();
            while (Char.IsDigit(currentChar))
            {
                sb.Append(currentChar);
                if (!NextChar())
                    throw new LoadException(UnexpectedEOF);
            }
            int targetCount;
            if (sb.Length == 0 || !int.TryParse(sb.ToString(), out targetCount))
                throw new LoadException(String.Format("ERROR, line {0}: Please, specify the target count for desired count condition", iterateLine + 1));
            condition.targetCount = targetCount;
            return condition;
        }

        /// <summary>
        /// '(' should have been just read at the time this method is called
        /// ( '(' being currentChar )
        /// </summary>
        private ConditionRaw ReadRawKindCondition(ref List<Location> relevantCells)
        {
            ConditionRaw condition = new ConditionRaw();
            condition.squares = new List<int>();
            condition.validColors = new List<string>();

            // Relevant cells:
            NextChar();
            while (true)
            {
                SkipWhiteSpace();
                if (currentChar.Equals(')'))
                    break;
                var index = ReadLocationIntoRelevantCells(ref relevantCells);
                condition.squares.Add(index);
                SkipWhiteSpace();
                if (!currentChar.Equals(',') && !currentChar.Equals(')'))
                    throw new LoadException(String.Format("ERROR, line {0}:" + Environment.NewLine +
                        "'{1}' is not a valid location separator, ',' expected (or ')' if all relevant cells were specified).",
                        iterateLine + 1, currentChar));
                if (currentChar.Equals(','))
                    NextChar();
            }
            if (condition.squares.Count == 0)
                throw new LoadException(String.Format("ERROR, line {0}: Square locations need to be specified for condition.", iterateLine + 1));
            
            NextChar();
            SkipWhiteSpace();
            var expectEquals = currentChar;
            NextChar();
            SkipWhiteSpace();
            if (!expectEquals.Equals('=') || !currentChar.Equals('('))
                throw new LoadException(String.Format("ERROR, line {0}: '=' expected, followed by desired square colors in parentheses",
                    iterateLine + 1, currentChar));
            NextChar();
            StringBuilder sb = new StringBuilder();
            
            // Relevant cell colors:
            while (true)
            {
                SkipWhiteSpace();
                if (currentChar.Equals(')'))
                    break;
                sb.Clear();
                while (Char.IsLetterOrDigit(currentChar) || currentChar.Equals('_'))
                {
                    sb.Append(currentChar);
                    if (!NextChar())
                        throw new LoadException(UnexpectedEOF);
                }
                if (sb.Length == 0)
                    throw new LoadException(String.Format("ERROR: line {0}: Invalid relevant colors definition in condition.", iterateLine + 1));
                condition.validColors.Add(sb.ToString());

                SkipWhiteSpace();
                if (!currentChar.Equals(',') && !currentChar.Equals(')'))
                    throw new LoadException(String.Format("ERROR, line {0}:" + Environment.NewLine +
                        "'{1}' is not a valid cell color name separator, ',' expected (or ')' if all relevant colors were specified).",
                        iterateLine + 1, currentChar));
                if (currentChar.Equals(','))
                    NextChar();
            }
            NextChar();
            if (condition.validColors.Count == 0)
                throw new LoadException(String.Format("ERROR, line {0}: No valid cell colors specified in condition.", iterateLine + 1));

            return condition;
        }

        /// <summary>
        /// Reads a Location and finds it among provided relevantCells, or adds the Location if
        /// it isn't present, returning its index in either case.
        /// </summary>
        /// <returns></returns>
        private int ReadLocationIntoRelevantCells(ref List<Location> relevantCells)
        {
            var location = ReadLocation();
            int? index = null;
            for (int i = 0; i < relevantCells.Count; ++i)
            {
                if (location.Equals(relevantCells[i]))
                {
                    index = i;
                    break;
                }
            }
            if (index == null)
            {
                index = relevantCells.Count;
                relevantCells.Add((Location)location);
            }
            return (int)index;
        }

        private Location ReadLocation()
        {
            string rightRaw, downRaw;
            int right, down;

            StringBuilder sb = new StringBuilder();
            SkipWhiteSpace();
            if (currentChar.Equals('-'))
            {
                sb.Append('-');
                NextChar();
            }
            while (Char.IsDigit(currentChar))
            {
                sb.Append(currentChar);
                if (!NextChar())
                    throw new LoadException(UnexpectedEOF);
            }
            if (sb.Length == 0)
                throw new LoadException(String.Format("ERROR: Invalid location definition in line {0}.", iterateLine + 1));
            rightRaw = sb.ToString();
            sb.Clear();
            SkipWhiteSpace();
            if (!currentChar.Equals(':'))
                throw new LoadException(String.Format("ERROR: Invalid location definition in line {0}.", iterateLine + 1));
            
            NextChar();
            SkipWhiteSpace();
            if (currentChar.Equals('-'))
            {
                sb.Append('-');
                NextChar();
            }
            while (Char.IsDigit(currentChar))
            {
                sb.Append(currentChar);
                NextChar();
            }
            downRaw = sb.ToString();
            if (sb.Length == 0 || !int.TryParse(rightRaw, out right) || !int.TryParse(downRaw, out down))
                throw new LoadException(String.Format("ERROR: Invalid location definition in line {0}.", iterateLine + 1));
            if (right < -8 || right > 8 || down < -8 || down > 8)
                throw new LoadException(String.Format("ERROR in location definition in line {0}:" + Environment.NewLine +
                    "Locations more than 8 squares far in any direction are forbidden.", iterateLine + 1));
            if (right == 0 && down == 0)
                throw new LoadException(String.Format("ERROR in location definition in line {0}:" + Environment.NewLine +
                    "Location 0:0 is forbidden.", iterateLine + 1));

            return new Location(right, down);
        }


        // MACRO LOADING:
        // First, lines starting with a '!' are found and their macros are read as raw text into
        // macroNames and macroRawSubstitutions (the validity of characters is checked)
        // 
        // Nested macros are legal, so we need to check no cycles occur and find the topological
        // sort of the relations. Both can be done simultaneously with a simple DFS topsort.
        // Instead of only having a boolean array telling us which nodes of our directed graph we
        // visited, we have an integer List macroSeenGeneration, which starts with (-1)s,
        // indicating 'unvisited' and stores the 'generation' they were visited - which increments
        // with each DFS subprogram. A cycle exists iff a node is visited twice in a single
        // generation. (no matter its node at which a DFS subprogram starts, all reachable nodes
        // will be reached and so the cycle will be detected.
        // We are still just doing the standard topsort though - its result doesn't need to be
        // saved, we can just perform CreateMacro on nodes on the way back in the DFS.
        // 
        // In the PrepareMacroGraph method, macroGraph is built, which is simply a neighbor list
        // representation of the directed graph at hand.. to find it, the raw texts are iterated
        // through and nested macros are marked in macroGraph, removed from the raw texts and also
        // marked in nestedMacros, which is just a list of positions and macro indexes that were
        // found, so that they can be substituted for the actual values in mentioned CreateMacro
        // method.

        private List<string> macroNames;
        private List<string> macroRawSubstitutions;
        private Dictionary<string, int> macroIdxs;     // The inverse of macroNames, storing the index in macroNames for a given name.
        private List<SortedSet<int>> macroGraph;
        private List<List<(int, int)>> nestedMacros;   // Tuples of position and macro index
        private List<int> macroSeenGeneration;

        private void LoadMacros()
        {
            macroNames = new List<string>();
            macroRawSubstitutions = new List<string>();
            macroIdxs = new Dictionary<string, int>();

            // First, just read the macro definitions:
            for (int line = 0; line < lines.Count; ++line)
            {
                if (lines[line].Length == 0)
                {
                    ignoreLines[line] = true;
                    continue;
                }
                if (!lines[line][0].Equals('!')) continue;
                ReadMacro(ref line);
            }

            PrepareMacroGraph();
            MacroTopsort();
        }

        private void ReadMacro(ref int line)
        {
            StringBuilder nameBuilder = new StringBuilder();
            StringBuilder substitutionBuilder = new StringBuilder();
            // states:
            // 0: skip '!'
            // 1: load name
            // 2: load substitution
            // 3: skip whitespace before substitution read
            // 4: '\' encountered (only legal in substitution read)
            // 5: '/' encountered (in substitution read, invalid in name)
            int state = 0;
            bool nextLine = true;
            while (nextLine)
            {
                ignoreLines[line] = true;
                nextLine = false;
                bool nextChar = true;
                if (line >= lines.Count) throw new LoadException(UnexpectedEOF);

                foreach (char c in lines[line])
                {
                    if (!nextChar) break;
                    switch (state)
                    {
                        case 0:
                            state = 1;
                            break;
                        case 1:
                            if (Char.IsWhiteSpace(c))
                            {
                                state = 3;
                                break;
                            }
                            if (!Char.IsLetterOrDigit(c) && !c.Equals('_'))
                                throw new LoadException(String.Format("ERROR, line {0}: Character {1} is not premitted here.", line + 1, c));
                            nameBuilder.Append(c);
                            break;
                        case 2:
                            if (!CharIsLegal(c))
                                throw new LoadException(String.Format("ERROR, line {0}: Character {1} is not premitted here.", line + 1, c));
                            if (c.Equals('/') || c.Equals('\\'))
                            {
                                state = c.Equals('/') ? 5 : 4;
                                break;
                            }
                            substitutionBuilder.Append(c);
                            break;
                        case 3:
                            if (Char.IsWhiteSpace(c)) break;
                            state = 2;
                            goto case 2;
                        case 4:
                            if (c.Equals('\\'))
                            {
                                substitutionBuilder.Append(Environment.NewLine);
                                nextChar = false;
                                nextLine = true;
                                state = 2;
                                ++line;
                                break;
                            }
                            throw new LoadException(String.Format("ERROR, line {0}: A single backslash (\\) is invalid.", line + 1));
                        case 5:
                            if (c.Equals('/'))
                            {
                                nextChar = false;
                                break;
                            }
                            throw new LoadException(String.Format("ERROR, line {0}: A single slash (/) is invalid.", line + 1));
                    }
                }
            }

            // Check macro has been read correctly:
            if (nameBuilder.Length == 0)
                throw new LoadException(String.Format("ERROR, line {0}: Empty macro definition.", line + 1));
            if (!Char.IsUpper(nameBuilder[0]))
                throw new LoadException(String.Format("ERROR, line {0}: A macro name must start with an uppercase letter.", line + 1));
            if (substitutionBuilder.Length == 0)
                throw new LoadException(String.Format("ERROR, line {0}: Macro substitutes to empty string.", line + 1));

            macroIdxs.Add(nameBuilder.ToString(), macroNames.Count);
            macroNames.Add(nameBuilder.ToString());
            macroRawSubstitutions.Add(substitutionBuilder.ToString());
        }

        private void PrepareMacroGraph()
        {
            macroGraph = new List<SortedSet<int>>();
            nestedMacros = new List<List<(int, int)>>();
            StringBuilder utilBuilder = new StringBuilder();
            StringBuilder substitutionBuilder = new StringBuilder();
            for (int idx = 0; idx < macroRawSubstitutions.Count; ++idx)
            {
                int position = 0;
                macroGraph.Add(new SortedSet<int>());
                nestedMacros.Add(new List<(int, int)>());

                substitutionBuilder.Clear();
                var substitutionEnumerator = macroRawSubstitutions[idx].GetEnumerator();
                substitutionEnumerator.MoveNext();
                bool iterate = true;
                while (iterate)
                {
                    if (Char.IsUpper(substitutionEnumerator.Current))
                    {
                        while (Char.IsLetterOrDigit(substitutionEnumerator.Current) || substitutionEnumerator.Current.Equals('_'))
                        {
                            utilBuilder.Append(substitutionEnumerator.Current);
                            if (!substitutionEnumerator.MoveNext())
                            {
                                iterate = false;
                                break;
                            }
                        }
                        int index;
                        string name = utilBuilder.ToString();
                        if (macroIdxs.TryGetValue(name, out index))
                        {
                            if (idx == index)
                                throw new LoadException(String.Format("ERROR: Macro {0} uses itself.", macroNames[idx]));
                            macroGraph[idx].Add(index);
                            nestedMacros[idx].Add((position, index));
                        }
                        else throw new LoadException(String.Format("ERROR in definition of macro {0}:" + Environment.NewLine +
                                                "Unknown nested macro '{1}'.", macroNames[idx], name));
                        utilBuilder.Clear();
                    }
                    else
                    {
                        // Not a macro, skip current sequence:
                        if (Char.IsLetterOrDigit(substitutionEnumerator.Current) || substitutionEnumerator.Current.Equals('_'))
                        {
                            while (Char.IsLetterOrDigit(substitutionEnumerator.Current) || substitutionEnumerator.Current.Equals('_'))
                            {
                                substitutionBuilder.Append(substitutionEnumerator.Current);
                                ++position;
                                if (!substitutionEnumerator.MoveNext())
                                {
                                    iterate = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            substitutionBuilder.Append(substitutionEnumerator.Current);
                            ++position;
                            if (!substitutionEnumerator.MoveNext())
                                iterate = false;
                        }
                    }
                }
                macroRawSubstitutions[idx] = substitutionBuilder.ToString();
            }
        }

        private void MacroTopsort()
        {
            macroSeenGeneration = new List<int>();
            for (int i = 0; i < macroNames.Count; ++i) macroSeenGeneration.Add(-1);

            int gen = 0;
            for (int idx = 0; idx < macroNames.Count; ++idx)
            {
                var result = RecurseTopsort(gen, idx);
                if (result != null)
                    throw new LoadException(result.ToString());
                ++gen;
            }
        }

        int firstInCycle;   // First index in found cycle
        bool firstCovered;  // Did we get to it recursing back up? (ie is it now time
                            // to just return the StringBuilder as is)
        /// <summary>
        /// If there is a cycle of nested macros, this returns a prepared StrignBuilder
        /// with an error message listing the cycle.
        /// </summary>
        private StringBuilder RecurseTopsort(int gen, int macroIdx)
        {
            if (macroSeenGeneration[macroIdx] == -1)
            {
                macroSeenGeneration[macroIdx] = gen;
                foreach (int neighbor in macroGraph[macroIdx])
                {
                    var result = RecurseTopsort(gen, neighbor);
                    if (result == null) continue;
                    // Cycle encountered:
                    if (firstCovered) return result;
                    if (firstInCycle == macroIdx)
                    {
                        firstCovered = true;
                        result.Insert(0, String.Format("ERROR: Macros are nested in a cyclic way. Found the following cycle: {0}{1}",
                            Environment.NewLine, macroNames[macroIdx]));
                        return result;
                    }
                    else
                    {
                        result.Insert(0, String.Format(" -> {0}", macroNames[macroIdx]));
                        return result;
                    }
                }
                CreateMacro(macroIdx);
            }
            else
            {
                if (macroSeenGeneration[macroIdx] < gen) return null;
                firstInCycle = macroIdx;
                firstCovered = false;
                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format(" -> {0}", macroNames[macroIdx]));
                return sb;
            }
            return null;
        }

        private void CreateMacro(int idx)
        {
            int position = 0;
            StringBuilder sb = new StringBuilder();
            var substitutionEnumerator = macroRawSubstitutions[idx].GetEnumerator();
            foreach (var pair in nestedMacros[idx])
            {
                while (position < pair.Item1)
                {
                    substitutionEnumerator.MoveNext();
                    sb.Append(substitutionEnumerator.Current);
                    ++position;
                }
                sb.Append(macroRawSubstitutions[pair.Item2]);
            }
            while (substitutionEnumerator.MoveNext())
                sb.Append(substitutionEnumerator.Current);

            macroRawSubstitutions[idx] = sb.ToString();
            macros.Add(macroNames[idx], macroRawSubstitutions[idx]);
        }


        private bool CharIsLegal(char c)
        {
            if (Char.IsWhiteSpace(c) || Char.IsLetterOrDigit(c)) return true;
            char[] otherLegal = { ',', '{', '}', '[', ']', '(', ')', '#', '<',
                                  '>', '=', '-', '+', ':', '\\', '/', '_' };
            foreach (char ol in otherLegal)
                if (c.Equals(ol)) return true;
            return false;
        }

        class CellColorRaw
        {
            public string name;
            public Color color;
            public List<Location> relevantCells;
            public List<RuleRaw> rules;
        }

        class RuleRaw
        {
            public List<ConditionRaw> conditions;
            public string resultColor;
        }

        class ConditionRaw
        {
            public List<int> squares;
            public List<string> validColors;
            public int? targetCount;
            public CountCondition.Ordering? order;
        }


        public class LoadException: Exception
        {
            public LoadException(string message) : base(message) { }
        }

        public class LoadResult
        {
            public Result result;
            public string message;

            public static LoadResult OkResult => new LoadResult
            {
                result = Result.OK,
                message = null
            };

            public static LoadResult ErrorResult(string message) => new LoadResult
            {
                result = Result.ERROR,
                message = message
            };

            public enum Result
            {
                OK, WARN, ERROR
            }
        }
    }
}
