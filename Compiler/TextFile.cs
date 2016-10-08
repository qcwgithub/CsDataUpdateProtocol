using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeDomCs2
{
    public class TextFile
    {
        public TextFile()
        {
        }
        public TextFile(TextFile par, string t)
        {
            parent = par;
            text = t;
        }
        TextFile parent;
        public string text;
        public string tag;

        List<TextFile> ch;
        public List<TextFile> Ch
        {
            get
            {
                if (ch == null)
                    ch = new List<TextFile>();
                return ch;
            }
        }

        public TextFile AddS(string format, params object[] args)
        {
            Ch.Add(new TextFile(this, 
                args.Length > 0 ? string.Format(null, format, args) : format
                ));
            return this;
        }
        public TextFile AddLine()
        {
            return AddS(" ");
        }
        public TextFile AddTag(string tag)
        {
            var c = new TextFile(this, "");
            c.tag = tag;
            Ch.Add(c);
            return this;
        }
        public TextFile FindByTag(string tag)
        {
            if (this.tag == tag)
                return this;

            if (ch != null)
            {   
                foreach (var c in ch)
                {
                    TextFile r = c.FindByTag(tag);
                    if (r != null)
                        return r;
                }
            }
            return null;
        }

        public TextFile BraceIn()
        {
            return AddS("{").In();
        }

        public TextFile In()
        {
            return Ch[Ch.Count - 1];
        }

        public TextFile BraceOut()
        {
            return Out().AddS("}");
        }
        public TextFile Out()
        {
            return parent;
        }

        public string Format(int layer = 0)
        {
            StringBuilder sb = new StringBuilder();

            FormatInternal(layer, sb);
            return sb.ToString();
        }

        void FormatInternal(int layer, StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(text))
            {
                sb.Append(text.PadLeft(text.Length + layer * 4, ' '));
            }

            if (ch != null)
            {
                foreach (var c in ch)
                {
                    if (!string.IsNullOrEmpty(c.text))
                        sb.AppendLine();
                    c.FormatInternal(layer + 1, sb);
                }
            }
        }
    }
}
