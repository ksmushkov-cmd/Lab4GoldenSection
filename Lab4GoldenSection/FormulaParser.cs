using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab4GoldenSection {
  public static class FormulaParser {
    public static double Eval(string expr, double x) {
      var p = new Parser(expr, x);
      double v = p.ParseExpression();
      p.SkipSpaces();
      if (!p.AtEnd)
        throw new FormatException("Лишние символы в формуле: " + p.Rest);
      return v;
    }

    private class Parser {
      private readonly string _s;
      private readonly double _x;
      private int _pos;

      public Parser(string s, double x) { _s = s ?? ""; _x = x; _pos = 0; }

      public bool AtEnd => _pos >= _s.Length;
      public string Rest => _s.Substring(Math.Min(_pos, _s.Length));

      public void SkipSpaces() {
        while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++;
      }

      public double ParseExpression() {
        double v = ParseTerm();
        while (true) {
          SkipSpaces();
          if (_pos >= _s.Length) break;
          char c = _s[_pos];
          if (c == '+') { _pos++; v += ParseTerm(); }
          else if (c == '-') { _pos++; v -= ParseTerm(); }
          else break;
        }
        return v;
      }

      private double ParseTerm() {
        double v = ParseFactor();
        while (true) {
          SkipSpaces();
          if (_pos >= _s.Length) break;
          char c = _s[_pos];
          if (c == '*') { _pos++; v *= ParseFactor(); }
          else if (c == '/') { _pos++; v /= ParseFactor(); }
          else break;
        }
        return v;
      }

      private double ParseFactor() {
        double v = ParseUnary();
        SkipSpaces();
        if (_pos < _s.Length && _s[_pos] == '^') {
          ++_pos;
          double exp = ParseFactor();
          v = Math.Pow(v, exp);
        }
        return v;
      }

      private double ParseUnary() {
        SkipSpaces();
        if (_pos < _s.Length && _s[_pos] == '+') { _pos++; return ParseUnary(); }
        if (_pos < _s.Length && _s[_pos] == '-') { _pos++; return -ParseUnary(); }
        return ParsePrimary();
      }

      private double ParsePrimary() {
        SkipSpaces();
        if (_pos >= _s.Length) throw new FormatException("Неожиданный конец формулы.");
        char c = _s[_pos];

        if (char.IsDigit(c) || c == '.') {
          int start = _pos;
          while (_pos < _s.Length &&
                (char.IsDigit(_s[_pos]) || _s[_pos] == '.' ||
                _s[_pos] == 'e' || _s[_pos] == 'E' ||
                ((_s[_pos] == '+' || _s[_pos] == '-') && _pos > start &&
                (_s[_pos - 1] == 'e' || _s[_pos - 1] == 'E'))))
            ++_pos;
            string num = _s.Substring(start, _pos - start).Replace(',', '.');
            return double.Parse(num, CultureInfo.InvariantCulture);
        }

        if (c == 'x' || c == 'X') { ++_pos; return _x; }

         if (c == '(') {
           ++_pos;
           double v = ParseExpression();
           SkipSpaces();
           if (_pos >= _s.Length || _s[_pos] != ')') throw new FormatException("Ожидалась ')'.");
             ++_pos;
           return v;
         }

         if (char.IsLetter(c)) {
           int start = _pos;
           while (_pos < _s.Length && char.IsLetter(_s[_pos])) _pos++;
           string name = _s.Substring(start, _pos - start).ToLowerInvariant();

           switch (name) {
             case "pi": return Math.PI;
             case "e": return Math.E;
           }

           SkipSpaces();
           if (_pos >= _s.Length || _s[_pos] != '(')
             throw new FormatException($"Ожидалась '(' после {name}.");
           ++_pos;
           double arg = ParseExpression();
           SkipSpaces();
           if (_pos >= _s.Length || _s[_pos] != ')') throw new FormatException("Ожидалась ')'.");
           ++_pos;

           switch (name) {
             case "sin": return Math.Sin(arg);
             case "cos": return Math.Cos(arg);
             case "tan": return Math.Tan(arg);
             case "sqrt": return Math.Sqrt(arg);
             case "exp": return Math.Exp(arg);
             case "log":
             case "ln": return Math.Log(arg);
             case "abs": return Math.Abs(arg);
             default: throw new FormatException("Неизвестная функция: " + name);
           }
         }

         throw new FormatException("Непонятный символ: " + c);
      }
    }
  }
}
