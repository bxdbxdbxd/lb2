using System.Text.RegularExpressions;

namespace WinFormsApp1
{
    public class LexicalAnalyzer
    {
        private List<TokenPattern> tokenPatterns;
        private List<string> validFunctions;

        public LexicalAnalyzer()
        {
            InitializeTokenPatterns();
            InitializeValidFunctions();
        }

        private void InitializeValidFunctions()
        {
            validFunctions = new List<string>
            {
                "parseFloat",
                "parseInt",
                "toString"
            };
        }

        private void InitializeTokenPatterns()
        {
            tokenPatterns = new List<TokenPattern>
            {
                new TokenPattern("KEYWORD", @"\blet\b"),
                new TokenPattern("KEYWORD", @"\bif\b"),
                new TokenPattern("KEYWORD", @"\belse\b"),
                new TokenPattern("KEYWORD", @"\bfor\b"),
                new TokenPattern("KEYWORD", @"\bwhile\b"),
                new TokenPattern("KEYWORD", @"\bdo\b"),
                new TokenPattern("KEYWORD", @"\bswitch\b"),
                new TokenPattern("KEYWORD", @"\bcase\b"),
                new TokenPattern("KEYWORD", @"\bbreak\b"),
                new TokenPattern("KEYWORD", @"\bcontinue\b"),
                new TokenPattern("KEYWORD", @"\breturn\b"),
                new TokenPattern("KEYWORD", @"\btrue\b"),
                new TokenPattern("KEYWORD", @"\bfalse\b"),
                new TokenPattern("KEYWORD", @"\bnull\b"),

                new TokenPattern("FUNCTION", @"\bparseFloat\b"),
                new TokenPattern("FUNCTION", @"\bparseInt\b"),
                new TokenPattern("FUNCTION", @"\btoString\b"),

                new TokenPattern("NUMBER", @"\b\d+\.\d+(?:[eE][+-]?\d+)?\b"),
                new TokenPattern("NUMBER", @"\b\d+(?:[eE][+-]?\d+)?\b"),

                new TokenPattern("STRING", @"'[^'\r\n]*'"),
                new TokenPattern("STRING", @"""[^""\r\n]*"""),

                new TokenPattern("OPERATOR", @"=="),
                new TokenPattern("OPERATOR", @"!="),
                new TokenPattern("OPERATOR", @"<="),
                new TokenPattern("OPERATOR", @">="),
                new TokenPattern("OPERATOR", @"&&"),
                new TokenPattern("OPERATOR", @"\|\|"),
                new TokenPattern("OPERATOR", @"\+\+"),
                new TokenPattern("OPERATOR", @"--"),
                new TokenPattern("OPERATOR", @"\*="),
                new TokenPattern("OPERATOR", @"/="),
                new TokenPattern("OPERATOR", @"\+="),
                new TokenPattern("OPERATOR", @"-="),
                new TokenPattern("OPERATOR", @"="),
                new TokenPattern("OPERATOR", @"<"),
                new TokenPattern("OPERATOR", @">"),
                new TokenPattern("OPERATOR", @"!"),
                new TokenPattern("OPERATOR", @"\+"),
                new TokenPattern("OPERATOR", @"-"),
                new TokenPattern("OPERATOR", @"\*"),
                new TokenPattern("OPERATOR", @"/"),
                new TokenPattern("OPERATOR", @"%"),

                new TokenPattern("PARENTHESIS", @"\("),
                new TokenPattern("PARENTHESIS", @"\)"),
                new TokenPattern("PARENTHESIS", @"\["),
                new TokenPattern("PARENTHESIS", @"\]"),
                new TokenPattern("PARENTHESIS", @"\{"),
                new TokenPattern("PARENTHESIS", @"\}"),
                new TokenPattern("SEMICOLON", @";"),
                new TokenPattern("DELIMITER", @","),
                new TokenPattern("DELIMITER", @"\."),
                new TokenPattern("DELIMITER", @":"),

                new TokenPattern("WHITESPACE", @"\s+"),

                new TokenPattern("IDENTIFIER", @"[a-zA-Z_][a-zA-Z0-9_]*"),
            };
        }

        public LexicalAnalysisResult Analyze(string text)
        {
            var result = new LexicalAnalysisResult();

            if (string.IsNullOrEmpty(text))
            {
                result.ErrorMessage = "Пустой ввод";
                result.SourceLines = new string[0];
                return result;
            }

            result.SourceLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string[] lines = result.SourceLines;
            int lineNum = 0;

            try
            {
                for (lineNum = 0; lineNum < lines.Length; lineNum++)
                {
                    string line = lines[lineNum];
                    AnalyzeLine(line, lineNum + 1, result);
                }

                CheckMissingSemicolon(result);
                CheckParenthesesBalance(result);
                CheckFunctionCalls(result);
                CheckVariableDeclarations(result);
                CheckUnknownFunctions(result);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new LexicalError
                {
                    Line = lineNum + 1,
                    Position = 1,
                    Message = $"Критическая ошибка анализа: {ex.Message}",
                    ErrorCode = "FATAL",
                    Severity = "КРИТИЧЕСКАЯ"
                });
            }

            return result;
        }

        private void AnalyzeLine(string line, int lineNumber, LexicalAnalysisResult result)
        {
            int pos = 0;
            int lineLength = line.Length;

            while (pos < lineLength)
            {
                bool matched = false;
                TokenPattern matchedPattern = null;
                Match matchedMatch = null;

                var sortedPatterns = tokenPatterns
                    .OrderByDescending(p => p.Regex.ToString().Length);

                foreach (var pattern in sortedPatterns)
                {
                    Match match = pattern.Regex.Match(line, pos);

                    if (match.Success && match.Index == pos)
                    {
                        matched = true;
                        matchedPattern = pattern;
                        matchedMatch = match;
                        break;
                    }
                }

                if (matched && matchedPattern != null && matchedMatch != null)
                {
                    string lexeme = matchedMatch.Value;
                    int startPos = pos;

                    if (matchedPattern.TokenType == "WHITESPACE")
                    {

                    }
                    else
                    {
                        result.Tokens.Add(new LexicalToken
                        {
                            Code = GetCodeForTokenType(matchedPattern.TokenType),
                            Type = GetNameForTokenType(matchedPattern.TokenType),
                            Lexeme = lexeme,
                            Line = lineNumber,
                            StartPosition = startPos + 1,
                            EndPosition = pos + lexeme.Length
                        });
                    }

                    pos = matchedMatch.Index + matchedMatch.Length;
                }
                else
                {
                    char invalidChar = line[pos];
                    string context = GetErrorContext(line, pos);

                    if (invalidChar != ' ' && invalidChar != '\t')
                    {
                        result.Errors.Add(new LexicalError
                        {
                            Line = lineNumber,
                            Position = pos + 1,
                            Character = invalidChar.ToString(),
                            Message = GetErrorMessageForInvalidChar(invalidChar, line, pos),
                            ErrorCode = "INVALID_CHAR",
                            Severity = "ОШИБКА",
                            Context = context
                        });
                    }

                    pos++;
                }
            }
        }

        private string GetErrorMessageForInvalidChar(char c, string line, int pos)
        {
            if (c >= 'А' && c <= 'Я' || c >= 'а' && c <= 'я' || c == 'ё' || c == 'Ё')
            {
                return $"Использование русской буквы '{c}' в коде. Используйте только латинские символы";
            }

            if (char.IsSymbol(c) || char.IsPunctuation(c))
            {
                string context = pos > 0 ? line.Substring(Math.Max(0, pos - 1), Math.Min(2, line.Length - pos)) : "";

                if (context == "&&" || context == "||" || context == "==" || context == "!=" ||
                    context == "<=" || context == ">=" || context == "++" || context == "--")
                {
                    return $"Некорректное использование оператора '{c}'";
                }

                return $"Недопустимый символ '{c}' в выражении";
            }

            return $"Недопустимый символ '{c}'";
        }

        private void CheckUnknownFunctions(LexicalAnalysisResult result)
        {
            var identifierTokens = result.Tokens.Where(t => t.Type == "идентификатор").ToList();

            foreach (var token in identifierTokens)
            {
                var nextTokens = result.Tokens
                    .Where(t => t.Line == token.Line && t.StartPosition > token.EndPosition)
                    .OrderBy(t => t.StartPosition)
                    .ToList();

                if (nextTokens.Count > 0 && nextTokens[0].Lexeme == "(")
                {
                    bool isValidFunction = validFunctions.Contains(token.Lexeme);

                    if (!isValidFunction)
                    {
                        string suggestion = GetClosestFunctionName(token.Lexeme);

                        result.Errors.Add(new LexicalError
                        {
                            Line = token.Line,
                            Position = token.StartPosition,
                            Character = token.Lexeme,
                            Message = $"Неизвестная функция '{token.Lexeme}'. Возможно, вы имели в виду '{suggestion}'?",
                            ErrorCode = "UNKNOWN_FUNCTION",
                            Severity = "ОШИБКА",
                            Context = GetErrorContext(result.SourceLines[token.Line - 1], token.StartPosition - 1)
                        });
                    }
                }
            }
        }

        private string GetClosestFunctionName(string wrongName)
        {
            Dictionary<string, string> commonMistakes = new Dictionary<string, string>
            {
                { "parseFloa", "parseFloat" },
                { "parseFlo", "parseFloat" },
                { "parsFloat", "parseFloat" },
                { "parsefloat", "parseFloat" },
                { "parseInte", "parseInt" },
                { "parsInt", "parseInt" },
                { "parseint", "parseInt" },
                { "toStrng", "toString" },
                { "tostring", "toString" },
                { "to string", "toString" }
            };

            if (commonMistakes.ContainsKey(wrongName))
                return commonMistakes[wrongName];

            foreach (string valid in validFunctions)
            {
                if (valid.StartsWith(wrongName) || wrongName.StartsWith(valid))
                    return valid;
            }

            return validFunctions[0];
        }

        private void CheckMissingSemicolon(LexicalAnalysisResult result)
        {
            if (result.SourceLines == null) return;

            for (int lineIdx = 0; lineIdx < result.SourceLines.Length; lineIdx++)
            {
                string line = result.SourceLines[lineIdx];
                int lineNumber = lineIdx + 1;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("//"))
                    continue;

                var tokensOnLine = result.Tokens.Where(t => t.Line == lineNumber).OrderBy(t => t.StartPosition).ToList();

                if (tokensOnLine.Count == 0)
                    continue;

                var lastToken = tokensOnLine.Last();

                bool hasSemicolon = lastToken.Lexeme == ";";
                bool hasOpeningBrace = lastToken.Lexeme == "{";
                bool hasClosingBrace = lastToken.Lexeme == "}";

                bool isControlFlow = tokensOnLine.Any(t =>
                    t.Lexeme == "if" || t.Lexeme == "else" ||
                    t.Lexeme == "for" || t.Lexeme == "while" ||
                    t.Lexeme == "do" || t.Lexeme == "switch" ||
                    t.Lexeme == "case");

                if (!hasSemicolon && !hasOpeningBrace && !hasClosingBrace && !isControlFlow)
                {
                    int position = line.Length + 1;
                    result.Errors.Add(new LexicalError
                    {
                        Line = lineNumber,
                        Position = position,
                        Character = ";",
                        Message = "Отсутствует точка с запятой ';' в конце выражения",
                        ErrorCode = "MISSING_SEMICOLON",
                        Severity = "ОШИБКА",
                        Context = line
                    });
                }
            }
        }

        private void CheckFunctionCalls(LexicalAnalysisResult result)
        {
            var functionTokens = result.Tokens.Where(t => t.Type == "функция").ToList();

            foreach (var funcToken in functionTokens)
            {
                var tokensAfter = result.Tokens
                    .Where(t => t.Line == funcToken.Line && t.StartPosition > funcToken.EndPosition)
                    .OrderBy(t => t.StartPosition)
                    .ToList();

                if (tokensAfter.Count == 0)
                {
                    result.Errors.Add(new LexicalError
                    {
                        Line = funcToken.Line,
                        Position = funcToken.EndPosition + 1,
                        Character = "",
                        Message = $"После функции '{funcToken.Lexeme}' ничего нет",
                        ErrorCode = "EMPTY_FUNCTION_CALL",
                        Severity = "ОШИБКА"
                    });
                    continue;
                }

                if (tokensAfter[0].Lexeme != "(")
                {
                    result.Errors.Add(new LexicalError
                    {
                        Line = funcToken.Line,
                        Position = funcToken.EndPosition + 1,
                        Character = tokensAfter[0].Lexeme,
                        Message = $"После функции '{funcToken.Lexeme}' ожидается открывающая скобка '(', а не '{tokensAfter[0].Lexeme}'",
                        ErrorCode = "MISSING_PAREN",
                        Severity = "ОШИБКА"
                    });
                    continue;
                }

                int parenLevel = 1;
                bool hasClosingParen = false;
                bool hasArgument = false;
                List<LexicalToken> arguments = new List<LexicalToken>();

                for (int i = 1; i < tokensAfter.Count; i++)
                {
                    var token = tokensAfter[i];

                    if (token.Lexeme == "(")
                    {
                        parenLevel++;
                    }
                    else if (token.Lexeme == ")")
                    {
                        parenLevel--;
                        if (parenLevel == 0)
                        {
                            hasClosingParen = true;
                            break;
                        }
                    }
                    else if (parenLevel == 1)
                    {
                        if (token.Lexeme != ",")
                        {
                            hasArgument = true;
                            arguments.Add(token);
                        }
                    }
                }

                if (!hasClosingParen)
                {
                    result.Errors.Add(new LexicalError
                    {
                        Line = funcToken.Line,
                        Position = funcToken.EndPosition + 1,
                        Character = "",
                        Message = $"Отсутствует закрывающая скобка в вызове функции '{funcToken.Lexeme}'",
                        ErrorCode = "MISSING_CLOSING_PAREN",
                        Severity = "ОШИБКА"
                    });
                }
                else if (!hasArgument)
                {
                    result.Errors.Add(new LexicalError
                    {
                        Line = funcToken.Line,
                        Position = funcToken.EndPosition + 2,
                        Character = "",
                        Message = $"Функция '{funcToken.Lexeme}' вызвана без аргументов",
                        ErrorCode = "EMPTY_ARGUMENTS",
                        Severity = "ОШИБКА"
                    });
                }
                else
                {
                    foreach (var arg in arguments)
                    {
                        if (funcToken.Lexeme == "parseFloat" || funcToken.Lexeme == "parseInt")
                        {
                            if (arg.Type != "строка" && arg.Type != "число")
                            {
                                result.Errors.Add(new LexicalError
                                {
                                    Line = arg.Line,
                                    Position = arg.StartPosition,
                                    Character = arg.Lexeme,
                                    Message = $"Функция '{funcToken.Lexeme}' ожидает строку или число в качестве аргумента, получено '{arg.Type}'",
                                    ErrorCode = "INVALID_ARGUMENT_TYPE",
                                    Severity = "ОШИБКА"
                                });
                            }

                            if (arg.Type == "строка")
                            {
                                string stringValue = arg.Lexeme.Trim('\'', '"');
                                if (funcToken.Lexeme == "parseFloat" && !IsValidFloatString(stringValue))
                                {
                                    result.Errors.Add(new LexicalError
                                    {
                                        Line = arg.Line,
                                        Position = arg.StartPosition,
                                        Character = arg.Lexeme,
                                        Message = $"Строка '{stringValue}' не является допустимым числом с плавающей точкой",
                                        ErrorCode = "INVALID_FLOAT_STRING",
                                        Severity = "ОШИБКА"
                                    });
                                }
                                else if (funcToken.Lexeme == "parseInt" && !IsValidIntString(stringValue))
                                {
                                    result.Errors.Add(new LexicalError
                                    {
                                        Line = arg.Line,
                                        Position = arg.StartPosition,
                                        Character = arg.Lexeme,
                                        Message = $"Строка '{stringValue}' не является допустимым целым числом",
                                        ErrorCode = "INVALID_INT_STRING",
                                        Severity = "ОШИБКА"
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsValidFloatString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            Regex floatRegex = new Regex(@"^-?\d+(\.\d+)?([eE][+-]?\d+)?$");
            return floatRegex.IsMatch(s);
        }

        private bool IsValidIntString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            Regex intRegex = new Regex(@"^-?\d+$");
            return intRegex.IsMatch(s);
        }

        private void CheckVariableDeclarations(LexicalAnalysisResult result)
        {
            var letTokens = result.Tokens.Where(t => t.Lexeme == "let").ToList();

            foreach (var letToken in letTokens)
            {
                var tokensOnLine = result.Tokens
                    .Where(t => t.Line == letToken.Line)
                    .OrderBy(t => t.StartPosition)
                    .ToList();

                int letIndex = tokensOnLine.IndexOf(letToken);

                if (letIndex + 1 >= tokensOnLine.Count)
                {
                    result.Errors.Add(new LexicalError
                    {
                        Line = letToken.Line,
                        Position = letToken.EndPosition + 1,
                        Character = "",
                        Message = "После 'let' должно следовать имя переменной",
                        ErrorCode = "MISSING_VARIABLE_NAME",
                        Severity = "ОШИБКА"
                    });
                    continue;
                }

                var nextToken = tokensOnLine[letIndex + 1];
                if (nextToken.Type != "идентификатор")
                {
                    result.Errors.Add(new LexicalError
                    {
                        Line = nextToken.Line,
                        Position = nextToken.StartPosition,
                        Character = nextToken.Lexeme,
                        Message = "Ожидается имя переменной после 'let'",
                        ErrorCode = "INVALID_VARIABLE_NAME",
                        Severity = "ОШИБКА"
                    });
                }

                bool hasEquals = false;
                int equalsIndex = -1;

                for (int i = letIndex + 2; i < tokensOnLine.Count; i++)
                {
                    if (tokensOnLine[i].Lexeme == "=")
                    {
                        hasEquals = true;
                        equalsIndex = i;
                        break;
                    }
                }

                if (!hasEquals)
                {
                    if (tokensOnLine.Count > letIndex + 2)
                    {
                        var tokenAfterVar = tokensOnLine[letIndex + 2];
                        result.Errors.Add(new LexicalError
                        {
                            Line = tokenAfterVar.Line,
                            Position = tokenAfterVar.StartPosition,
                            Character = tokenAfterVar.Lexeme,
                            Message = "Отсутствует оператор присваивания '='",
                            ErrorCode = "MISSING_ASSIGNMENT",
                            Severity = "ОШИБКА"
                        });
                    }
                    else
                    {
                        result.Errors.Add(new LexicalError
                        {
                            Line = nextToken.Line,
                            Position = nextToken.EndPosition + 1,
                            Character = "=",
                            Message = "Отсутствует оператор присваивания '='",
                            ErrorCode = "MISSING_ASSIGNMENT",
                            Severity = "ОШИБКА"
                        });
                    }
                }
                else
                {
                    if (equalsIndex + 1 >= tokensOnLine.Count)
                    {
                        result.Errors.Add(new LexicalError
                        {
                            Line = tokensOnLine[equalsIndex].Line,
                            Position = tokensOnLine[equalsIndex].EndPosition + 1,
                            Character = "",
                            Message = "Отсутствует значение после оператора присваивания",
                            ErrorCode = "MISSING_VALUE",
                            Severity = "ОШИБКА"
                        });
                    }
                    else
                    {
                        var valueToken = tokensOnLine[equalsIndex + 1];

                        if (valueToken.Lexeme == ";")
                        {
                            result.Errors.Add(new LexicalError
                            {
                                Line = valueToken.Line,
                                Position = valueToken.StartPosition,
                                Character = valueToken.Lexeme,
                                Message = "Отсутствует значение после оператора присваивания",
                                ErrorCode = "MISSING_VALUE",
                                Severity = "ОШИБКА"
                            });
                        }
                    }
                }
            }
        }

        private void CheckParenthesesBalance(LexicalAnalysisResult result)
        {
            var parentheses = new Stack<(char type, int line, int pos)>();
            var brackets = new Stack<(char type, int line, int pos)>();
            var braces = new Stack<(char type, int line, int pos)>();

            foreach (var token in result.Tokens.Where(t => t.Type == "скобка"))
            {
                int line = token.Line;
                int pos = token.StartPosition;

                switch (token.Lexeme)
                {
                    case "(":
                        parentheses.Push(('(', line, pos));
                        break;
                    case ")":
                        if (parentheses.Count == 0)
                        {
                            result.Errors.Add(new LexicalError
                            {
                                Line = line,
                                Position = pos,
                                Character = ")",
                                Message = "Неожиданная закрывающая круглая скобка",
                                ErrorCode = "UNEXPECTED_PAREN",
                                Severity = "ОШИБКА"
                            });
                        }
                        else
                        {
                            parentheses.Pop();
                        }
                        break;
                    case "[":
                        brackets.Push(('[', line, pos));
                        break;
                    case "]":
                        if (brackets.Count == 0)
                        {
                            result.Errors.Add(new LexicalError
                            {
                                Line = line,
                                Position = pos,
                                Character = "]",
                                Message = "Неожиданная закрывающая квадратная скобка",
                                ErrorCode = "UNEXPECTED_BRACKET",
                                Severity = "ОШИБКА"
                            });
                        }
                        else
                        {
                            brackets.Pop();
                        }
                        break;
                    case "{":
                        braces.Push(('{', line, pos));
                        break;
                    case "}":
                        if (braces.Count == 0)
                        {
                            result.Errors.Add(new LexicalError
                            {
                                Line = line,
                                Position = pos,
                                Character = "}",
                                Message = "Неожиданная закрывающая фигурная скобка",
                                ErrorCode = "UNEXPECTED_BRACE",
                                Severity = "ОШИБКА"
                            });
                        }
                        else
                        {
                            braces.Pop();
                        }
                        break;
                }
            }

            foreach (var p in parentheses)
            {
                result.Errors.Add(new LexicalError
                {
                    Line = p.line,
                    Position = p.pos,
                    Message = "Незакрытая круглая скобка '('",
                    ErrorCode = "UNCLOSED_PAREN",
                    Severity = "ОШИБКА"
                });
            }

            foreach (var b in brackets)
            {
                result.Errors.Add(new LexicalError
                {
                    Line = b.line,
                    Position = b.pos,
                    Message = "Незакрытая квадратная скобка '['",
                    ErrorCode = "UNCLOSED_BRACKET",
                    Severity = "ОШИБКА"
                });
            }

            foreach (var b in braces)
            {
                result.Errors.Add(new LexicalError
                {
                    Line = b.line,
                    Position = b.pos,
                    Message = "Незакрытая фигурная скобка '{'",
                    ErrorCode = "UNCLOSED_BRACE",
                    Severity = "ОШИБКА"
                });
            }
        }

        private string GetErrorContext(string line, int position, int contextLength = 20)
        {
            int start = Math.Max(0, position - contextLength / 2);
            int length = Math.Min(contextLength, line.Length - start);

            if (length <= 0) return "";

            string context = line.Substring(start, length);
            if (start > 0) context = "..." + context;
            if (start + length < line.Length) context = context + "...";

            return context;
        }

        private int GetCodeForTokenType(string tokenType)
        {
            return tokenType switch
            {
                "KEYWORD" => 14,
                "IDENTIFIER" => 2,
                "NUMBER" => 1,
                "OPERATOR" => 10,
                "STRING" => 3,
                "FUNCTION" => 7,
                "PARENTHESIS" => 8,
                "SEMICOLON" => 16,
                "DELIMITER" => 11,
                _ => 99
            };
        }

        private string GetNameForTokenType(string tokenType)
        {
            return tokenType switch
            {
                "KEYWORD" => "ключевое слово",
                "IDENTIFIER" => "идентификатор",
                "NUMBER" => "число",
                "OPERATOR" => "оператор",
                "STRING" => "строка",
                "FUNCTION" => "функция",
                "PARENTHESIS" => "скобка",
                "SEMICOLON" => "конец оператора",
                "DELIMITER" => "разделитель",
                _ => "неизвестный символ"
            };
        }

        private class TokenPattern
        {
            public string TokenType { get; set; }
            public Regex Regex { get; set; }

            public TokenPattern(string tokenType, string pattern)
            {
                TokenType = tokenType;
                Regex = new Regex(pattern, RegexOptions.Compiled);
            }
        }
    }

    public class LexicalAnalysisResult
    {
        public List<LexicalToken> Tokens { get; set; } = new List<LexicalToken>();
        public List<LexicalError> Errors { get; set; } = new List<LexicalError>();
        public string ErrorMessage { get; set; }
        public string[] SourceLines { get; set; }
    }

    public class LexicalToken
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public string Lexeme { get; set; }
        public int Line { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public string Location => $"строка {Line}, {StartPosition}-{EndPosition}";
    }

    public class LexicalError
    {
        public int Line { get; set; }
        public int Position { get; set; }
        public string Character { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; } = "ERR";
        public string Severity { get; set; } = "ОШИБКА";
        public string Context { get; set; }
    }
}