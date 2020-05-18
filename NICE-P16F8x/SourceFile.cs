using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NICE_P16F8x
{
    class SourceFile
    {
        ObservableCollection<SourceLine> sourceLines = new ObservableCollection<SourceLine>();
        int[] linesWithCommands;
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
                bool hasCommand = false;
                //Check if line contains command, save it and remove it from the list
                string comment = "";
                string[] lineCommentSplit = line.Split(";");
                if (lineCommentSplit.Length > 1)
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
                        command = string.Join(" ", lineComponents.Skip(1).ToArray());
                    }

                    //Add line to list of SourceLines
                    SourceLine sl = new SourceLine(lineNumber, command, comment, hasCommand);
                    sourceLines.Add(sl);
                }
                lineCounter++;
            }
            file.Close();

            this.linesWithCommands = linesWithCommands.ToArray();

            //Write commands to Data store
            Data.setWriteProgram(commands);
        }
        public void HighlightLine(int pcl)
        {
            sourceLines[getSourceLineIndexFromPCL(LastHighlightedIndex)].Active = false;
            sourceLines[getSourceLineIndexFromPCL(pcl)].Active = true;
            LastHighlightedIndex = pcl;
        }
        public ObservableCollection<SourceLine> getSourceLines()
        {
            return sourceLines;
        }
        public int getSourceLineIndexFromPCL(int pcl)
        {
            if (pcl < linesWithCommands.Length) return linesWithCommands[pcl];
            else return -1;

        }
    }
    class SourceLine : ObservableObject
    {
        public bool Breakpoint { get; set; }
        public string LineNumber { get; set; }
        public string Command { get; set; }
        public string Comment { get; set; }
        public bool hasCommand { get; set; }
        private bool active;
        public bool Active
        {
            get { return this.active; }
            set { this.SetAndNotify(ref this.active, value, () => this.Active); }
        }

        public SourceLine(string lineNumber, string command, string comment, bool hasCommand)
        {
            this.Breakpoint = false;
            this.LineNumber = lineNumber;
            this.Command = command;
            this.Comment = comment;
            this.hasCommand = hasCommand;
            this.active = false;
        }
    }
}
