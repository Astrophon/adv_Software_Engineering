using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NICE_P16F8x
{
    class SourceFile
    {
        readonly ObservableCollection<SourceLine> sourceLines = new ObservableCollection<SourceLine>();
        readonly int[] linesWithCommands;
        int LastHighlightedIndex = 0;

        public SourceFile(string path)
        {
            List<string> commands = new List<string>();
            List<int> linesWithCommands = new List<int>();

            int lineCounter = 0;
            string line;

            System.IO.StreamReader file =
                new System.IO.StreamReader(@path, CodePagesEncodingProvider.Instance.GetEncoding(1252));
            while ((line = file.ReadLine()) != null)
            {
                //Check if line contains command, save it and remove it from the list
                string comment = "";
                string[] lineCommentSplit = line.Split(";");
                if (lineCommentSplit.Length > 1)
                {
                    comment = lineCommentSplit[1];
                    line = lineCommentSplit[0];
                }
                //Check if line contains label, save it
                string label;
                bool hasLabel = false;
                label = line.Substring(27).Split(" ")[0];
                if (label != "") hasLabel = true;

                //Split line in substrings, seperated by " ", removing all duplicate white spaces
                string[] lineComponents = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                //Check if line contains hexadecimal command
                bool hasCommand = false;
                if (!char.IsWhiteSpace(line, 0))
                {
                    commands.Add(lineComponents[1]);
                    lineComponents = lineComponents.Skip(2).ToArray();
                    linesWithCommands.Add(lineCounter);
                    hasCommand = true;
                }

                //Fill lineNumber and command variable
                if (lineComponents.Length > 0)
                {
                    string lineNumber = lineComponents[0];
                    string command = "";
                    if (lineComponents.Length > 1)
                    {
                        command = string.Join(" ", lineComponents.Skip(hasLabel ? 2 : 1).ToArray());
                    }

                    //Add line to list of SourceLines
                    SourceLine sl = new SourceLine(lineNumber, label, command, comment, hasCommand);
                    sourceLines.Add(sl);
                }
                lineCounter++;
            }
            file.Close();

            this.linesWithCommands = linesWithCommands.ToArray();

            //Write commands to Data store
            Data.SetWriteProgram(commands);
        }
        public void HighlightLine(int pc)
        {
            sourceLines[GetSourceLineIndexFromPC(LastHighlightedIndex)].Active = false;
            sourceLines[GetSourceLineIndexFromPC(pc)].Active = true;
            LastHighlightedIndex = pc;
        }
        public ObservableCollection<SourceLine> GetSourceLines()
        {
            return sourceLines;
        }
        public int GetSourceLineIndexFromPC(int pc)
        {
            if (pc < linesWithCommands.Length) return linesWithCommands[pc];
            else return -1;

        }
        public bool LineHasBreakpoint(int pc)
        {
            return sourceLines[GetSourceLineIndexFromPC(pc)].Breakpoint;
        }
    }
    class SourceLine : ObservableObject
    {
        public string LineNumber { get; set; }
        public string Label { get; set; }
        public string Command { get; set; }
        public string Comment { get; set; }
        public bool HasCommand { get; set; }
        private bool active;
        public bool Active
        {
            get { return active; }
            set { SetAndNotify(ref active, value, () => Active); }
        }
        private bool breakpoint;
        public bool Breakpoint
        {
            get { return breakpoint; }
            set { SetAndNotify(ref breakpoint, value, () => Breakpoint); }
        }

        public SourceLine(string lineNumber, string label, string command, string comment, bool hasCommand)
        {
            breakpoint = false;
            LineNumber = lineNumber;
            Label = label;
            Command = command;
            Comment = comment;
            HasCommand = hasCommand;
            active = false;
        }
    }
}
