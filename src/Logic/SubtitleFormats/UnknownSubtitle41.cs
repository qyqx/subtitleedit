﻿using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class UnknownSubtitle41 : SubtitleFormat
    {

        static Regex regexTimeCodes1 = new Regex(@"^\d+.\d$", RegexOptions.Compiled);
        static Regex regexTimeCodes2 = new Regex(@"^\d+.\d\d$", RegexOptions.Compiled);

        public override string Extension
        {
            get { return ".txt"; }
        }

        public override string Name
        {
            get { return "Unknown 41"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            Subtitle subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            const string paragraphWriteFormat = "{0}\r\n{1}\r\n{2}\r\n";
            var sb = new StringBuilder();
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                sb.AppendLine(string.Format(paragraphWriteFormat, EncodeTimeCode(p.StartTime), p.Text, EncodeTimeCode(p.EndTime)));
            }
            return sb.ToString().Trim();
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            //911.2
            //C’est l’enfant qui l’a tuée ?
            //915.8/

            //921.8
            //Comment elle s’appelait ?
            //924.6/

            _errorCount = 0;
            Paragraph p = null;
            bool textOn = false;
            var sb = new StringBuilder();
            foreach (string line in lines)
            {
                try
                {
                    if (textOn)
                    {
                        if (regexTimeCodes1.Match(line.TrimEnd('/')).Success || regexTimeCodes2.Match(line).Success)
                        {
                            p.EndTime = DecodeTimeCode(line.TrimEnd('/').Split('.'));
                            if (sb.Length > 0)
                            {
                                p.Text = sb.ToString().TrimEnd();
                                subtitle.Paragraphs.Add(p);
                                textOn = false;
                            }
                        }
                        else
                        {
                            sb.AppendLine(line);
                            if (sb.Length > 500)
                            {
                                _errorCount += 10;
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (regexTimeCodes1.Match(line).Success || regexTimeCodes2.Match(line).Success)
                        {
                            p = new Paragraph();
                            sb = new StringBuilder();
                            p.StartTime = DecodeTimeCode(line.Split('.'));
                            textOn = true;
                        }
                    }
                }
                catch
                {
                    textOn = false;
                    _errorCount++;
                }
            }
            if (textOn && sb.Length > 0 && p != null)
            {
                p.Text = sb.ToString().TrimEnd();
                subtitle.Paragraphs.Add(p);
            }
            subtitle.Renumber(1);
        }

        private string EncodeTimeCode(TimeCode time)
        {
            Configuration.Settings.General.CurrentFrameRate = 24.0;
            int frames = MillisecondsToFrames(time.TotalMilliseconds);
            int footage = frames / 16;
            int rest = (int)((frames % 16) / 16.0 * Configuration.Settings.General.CurrentFrameRate);
            return string.Format("{0}.{1:0}", footage, rest);
        }

        private TimeCode DecodeTimeCode(string[] parts)
        {
            Configuration.Settings.General.CurrentFrameRate = 24.0;
            string frames16 = parts[0];
            string frames = parts[1];
            return new TimeCode(0, 0, 0, FramesToMilliseconds(16 * int.Parse(frames16) + int.Parse(frames)));
        }

    }
}
