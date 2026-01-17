using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniJSON
{
    public static class Json
    {
        public static object Deserialize(string json)
        {
            if (json == null)
                return null;
            return Parser.Parse(json);
        }

        sealed class Parser : IDisposable
        {
            const string k_WordBreak = "{}[],:\"";

            public static bool IsWordBreak(char c)
            {
                return char.IsWhiteSpace(c) || k_WordBreak.IndexOf(c) != -1;
            }

            StringReader m_Reader;

            Parser(string json)
            {
                m_Reader = new StringReader(json);
            }

            public static object Parse(string json)
            {
                using (var instance = new Parser(json))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                m_Reader.Dispose();
                m_Reader = null;
            }

            Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();

                m_Reader.Read();

                while (true)
                {
                    switch (NextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.CurlyClose:
                            return table;
                        case Token.Comma:
                            continue;
                        default:
                            string name = ParseString();
                            if (name == null)
                                return null;

                            if (NextToken != Token.Colon)
                                return null;
                            m_Reader.Read();

                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            List<object> ParseArray()
            {
                var array = new List<object>();
                m_Reader.Read();

                bool parsing = true;
                while (parsing)
                {
                    Token nextToken = NextToken;

                    switch (nextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.SquareClose:
                            parsing = false;
                            break;
                        case Token.Comma:
                            break;
                        default:
                            object value = ParseValue();
                            array.Add(value);
                            break;
                    }
                }

                return array;
            }

            object ParseValue()
            {
                switch (NextToken)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquareOpen:
                        return ParseArray();
                    case Token.True:
                        return true;
                    case Token.False:
                        return false;
                    case Token.Null:
                        return null;
                    default:
                        return null;
                }
            }

            string ParseString()
            {
                var s = new StringBuilder();
                char c;

                m_Reader.Read();

                bool parsing = true;
                while (parsing)
                {
                    if (m_Reader.Peek() == -1)
                        break;

                    c = NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (m_Reader.Peek() == -1)
                                parsing = false;
                            else
                            {
                                c = NextChar;
                                switch (c)
                                {
                                    case '"':
                                    case '\\':
                                    case '/':
                                        s.Append(c);
                                        break;
                                    case 'b':
                                        s.Append('\b');
                                        break;
                                    case 'f':
                                        s.Append('\f');
                                        break;
                                    case 'n':
                                        s.Append('\n');
                                        break;
                                    case 'r':
                                        s.Append('\r');
                                        break;
                                    case 't':
                                        s.Append('\t');
                                        break;
                                    case 'u':
                                        var hex = new char[4];
                                        for (int i = 0; i < 4; i++)
                                        {
                                            hex[i] = NextChar;
                                        }
                                        s.Append((char)Convert.ToInt32(new string(hex), 16));
                                        break;
                                }
                            }
                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;

                if (number.IndexOf('.') == -1 && number.IndexOf('e') == -1 && number.IndexOf('E') == -1)
                {
                    if (long.TryParse(number, out long parsedInt))
                        return parsedInt;
                }

                if (double.TryParse(number, out double parsedDouble))
                    return parsedDouble;

                return 0d;
            }

            void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar))
                {
                    m_Reader.Read();
                    if (m_Reader.Peek() == -1)
                        break;
                }
            }

            char PeekChar => Convert.ToChar(m_Reader.Peek());

            char NextChar => Convert.ToChar(m_Reader.Read());

            string NextWord
            {
                get
                {
                    var word = new StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);
                        if (m_Reader.Peek() == -1)
                            break;
                    }
                    return word.ToString();
                }
            }

            Token NextToken
            {
                get
                {
                    EatWhitespace();
                    if (m_Reader.Peek() == -1)
                        return Token.None;

                    char c = PeekChar;
                    switch (c)
                    {
                        case '{':
                            return Token.CurlyOpen;
                        case '}':
                            m_Reader.Read();
                            return Token.CurlyClose;
                        case '[':
                            return Token.SquareOpen;
                        case ']':
                            m_Reader.Read();
                            return Token.SquareClose;
                        case ',':
                            m_Reader.Read();
                            return Token.Comma;
                        case '"':
                            return Token.String;
                        case ':':
                            return Token.Colon;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return Token.Number;
                    }

                    string word = NextWord;
                    switch (word)
                    {
                        case "false":
                            return Token.False;
                        case "true":
                            return Token.True;
                        case "null":
                            return Token.Null;
                    }

                    return Token.None;
                }
            }

            enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquareOpen,
                SquareClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null
            }
        }
    }
}
