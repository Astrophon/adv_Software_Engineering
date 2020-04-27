using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NICE_P16F8x
{
    class SourceFile
    {
        List<SourceLine> sourceLines = new List<SourceLine>();
        public SourceFile(string path)
        {
            List<string> commands = new List<string>();
            int counter = 0;
            string line;

            System.IO.StreamReader file =
                new System.IO.StreamReader(@path, CodePagesEncodingProvider.Instance.GetEncoding(1252));
            while ((line = file.ReadLine()) != null)
            {
                //Check if line contains command, save it and remove it from the list
                string comment = "";
                string[] lineCommentSplit = line.Split(";");
                if(lineCommentSplit.Length > 1)
                {
                    comment = lineCommentSplit[1];
                    line = lineCommentSplit[0];
                }
                //Split line in substrings, seperated by " ", removing all duplicate white spaces
                string[] lineComponents = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                //Check if line contains hexadecimal command
                if (!char.IsWhiteSpace(line, 0))
                {
                    commands.Add(lineComponents[1]);
                    lineComponents = lineComponents.Skip(2).ToArray();
                }

                //Fill lineNumber and command variable
                string lineNumber = lineComponents[0];
                string command = "";
                if (lineComponents.Length > 1)
                {
                    command = string.Join(" ", lineComponents.Skip(1).ToArray());
                }

                //Add line to list of SourceLines
                SourceLine sl = new SourceLine(lineNumber, command, comment);
                sourceLines.Add(sl);
                counter++;
            }

            file.Close();

            //Write commands to Data Store BROKEN RIGHT NOW
            //Data.setWriteProgram(commands);
        }
        public List<SourceLine> getSourceLines()
        {
            return sourceLines;
        }
    }

 

    class SourceLine
    {
        public string Line { get; set; }
        public string Command { get; set; }
        public string Comment { get; set; }

        public SourceLine(string line, string command, string comment)
        {
            this.Line = line;
            this.Command = command;
            this.Comment = comment;
        }
    }
}
