﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DevTools
{
    internal class Variables
    {
        public static bool VariableExists(string name)
        {
            foreach (var line in DefineVariableContents())
            {
                if (line.Split(',')[0] == name) //Does it exist?
                {
                    return true;
                }
            }
            foreach (var line in File.ReadAllLines(FuncFilePath))
            {
                if (line.Split('(')[0] == name) //Does it exist?
                {
                    return true;
                }
            }
            foreach (var variable in tempVariables.Keys)
            {
                if (variable == name) //Does it exist?
                {
                    return true;
                }
            }
            return false;
        }
        static void DefineVariable(string variable)
        {
            string[] strings = variable.SplitAtFirst('=');
            int equalsIDX = strings[0].Length - 1;
            string value = strings[1];
            string variableName = variable.Substring(7, equalsIDX - 6);
            if (variableName.Any(c => !char.IsLetter(c) && c != ' '))
            {
                PrintColour("Invalid variable name", false);
                return;
            }
            if (VariableExists(variableName))
            {
                PrintError("Variable is already defined");
                return;
            }
            List<string> contents = DefineVariableContents(variableName, value);
            if (contents.All(s => s.SplitAtFirst(',')[0] != variableName))
            {
                contents.Add(string.Format("{0},{1}", variableName, value));
            }
            File.WriteAllLines(DataFilePath, contents);
        }
        public static List<string> DefineVariableContents(string variableName = "", string value = "")
        {
            List<string> contents = File.ReadAllLines(DataFilePath).ToList();
            for (int i = 0; i < contents.Count; i++)
            {
                string s = contents[i];
                string[] args = s.SplitAtFirst(',');
                if (args[0] == variableName)
                {
                    var ss = s.SplitAtFirst(',');
                    ss[1] = value;
                    contents[i] = ss[0] + ',' + ss[1];
                    File.WriteAllLines(DataFilePath, contents);
                }
            }

            return contents;
        }
        static void DefineFunction(string function)
        {
            function = function.Substring("#defunc".Length);

            var prev = File.ReadAllLines(FuncFilePath).ToList();
            var name = function.Substring(0, function.IndexOf('('));
            if (VariableExists(name))
            {
                PrintError("Variable is already defined");
                return;
            }
            List<string> toremove = new List<string>();
            foreach (var s in prev)
            {
                if (s == "SYSTEM FUNCTIONS:")
                {
                    continue;
                }
                var bracketidx = s.IndexOf('(');
                var substr = s.Substring(0, bracketidx);
                if (substr == name) //Already defined function
                {
                    toremove.Add(s);
                }
            }
            foreach (var s in toremove)
            {
                prev.Remove(s);
            }
            File.WriteAllLines(FuncFilePath, prev.ToArray());

            if (!function.Contains(')'))
            {
                expectingError = true;
                throw new Exception("Wrong formatting for declaring function.\nDid not include closing bracket");
            }
            if (function.Split(')')[1].Length == 0)
            {
                expectingError = true;
                throw new Exception("Define an action to take place with the variable");
            }
            string result = function + "\n" + File.ReadAllText(FuncFilePath);

            File.WriteAllText(FuncFilePath, result);
        }
        static void DeleteFunction(string name)
        {
            var prev = File.ReadAllLines(FuncFilePath).ToList();
            List<string> toremove = new List<string>();
            foreach (var s in prev)
            {
                if (s == "SYSTEM FUNCTIONS:")
                {
                    break;
                }
                var bracketidx = s.IndexOf('(');
                var substr = s.Substring(0, bracketidx);
                if (substr == name) //Already defined function
                {
                    toremove.Add(s);
                }
            }
            foreach (var s in toremove)
            {
                prev.Remove(s);
            }
            File.WriteAllLines(FuncFilePath, prev.ToArray());
        }
        static void PrintDescription(string name)
        {
            var prev = File.ReadAllLines(FuncFilePath).ToList();
            var nextidx = 0;
            for (int i = 0; i < prev.Count; i++)
            {
                string? s = prev[i];
                if (s == "SYSTEM FUNCTIONS:")
                {
                    nextidx = i + 1;
                    break;
                }
                var bracketidx = s.IndexOf('(');
                var substr = s.Substring(0, bracketidx);
                if (substr == name) //Already defined function
                {
                    ShowDescription(s);
                }
            }
            for (int i = nextidx; i < prev.Count; i++)
            {
                string? s = prev[i];

                var bracketidx = s.IndexOf('(');
                var substr = s.Substring(0, bracketidx);
                if (substr == name) //Already defined function
                {
                    ShowDescription(s);
                }
            }
        }
        static void PrintDescription()
        {
            var prev = File.ReadAllLines(FuncFilePath).ToList();
            foreach (var s in prev)
            {
                PrintColour(s + ":");
                ShowDescription(s);
            }
        }

        /// <summary>
        /// Replaces 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="variableName"></param>
        /// <param name="variableValue"></param>
        /// <returns></returns>
        private static string ReplaceTempVariables(string input, char variableName, string variableValue)
        {
            string result = "";
            for (int i = 0; i < input.Length; ++i)
            {
                char c = input[i];
                if (c == variableName)
                {
                    if ((input.Length - 1 == i || IsOperator(input[i + 1])) && (i == 0 || IsOperator(input[i - 1])))
                    {
                        result += variableValue;
                    }
                    else if (i == 2 && (input.StartsWith("nw") || input.StartsWith("np"))) //Is there a nw or a np command?
                    {
                        //Replace the variable
                        result += variableValue;
                    }
                    else if (i == 4 && input.StartsWith("nwnp")) //nw and np?
                    {
                        //replace the variable
                        result += variableValue;
                    }
                    else
                    {
                        result += c;
                    }
                }
                else
                {
                    result += c;
                }
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="variableName"></param>
        /// <param name="variableValue"></param>
        /// <returns></returns>
        private static string ReplaceTempVariables(string input, string variableName, string variableValue)
        {
            string result = "";
            for (int i = 0; i < input.Length; ++i)
            {
                char c = input[i];
                string currentVar = "";
                result += c;
                if (result.Length >= variableName.Length && !IsOperator(input[i]) && !char.IsDigit(input[i]))
                {
                    currentVar = result.Substring(i - variableName.Length + 1);
                }
                if (currentVar == variableName) //Is this the current variable we are working on
                {
                    if (input.Length == result.Length //Are we at the end of the line?
                    || (IsOperator(input[i + 1]) //At the end of the variable. i.e. there isn't a letter or number after the variable, but an operator
                    && (i == variableName.Length - 1 || IsOperator(input[i - variableName.Length]))))

                    {
                        result = result.Substring(0, result.Length - variableName.Length) + input.Substring(i + 1, input.Length - 1 - i);
                        result = result.Insert(i - variableName.Length + 1, "(" + variableValue + ")");
                        PrintColour(variableName + " --> " + variableValue, true);
                        return ReplaceTempVariables(result, variableName, variableValue);
                    }
                }
            }
            return result;
        }
        private static bool ContainsVariable(string input, string variableName)
        {
            string result = "";
            for (int i = 0; i < input.Length; ++i)
            {
                char c = input[i];
                string currentVar = "";
                result += c;
                if (result.Length >= variableName.Length)
                {
                    currentVar = result.Substring(i - variableName.Length + 1);
                }
                if (currentVar == variableName && (input.Length - result.Length == 0 || IsOperator(input[i + 1])) && (i == variableName.Length - 1 || IsOperator(input[i - variableName.Length])))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// See if user is modifying a variable that already exists
        /// </summary>
        /// <param name="userINPUT">users input</param>
        /// <returns></returns>
        public static bool ModifyVariables(string userINPUT)
        {
            bool mainresult = false;
            if (userINPUT.Contains('=') && userINPUT.Substring(0, 3) != "var")
            {
                Dictionary<string, string> toreplace = new Dictionary<string, string>();
                List<string> tonull = new List<string>();
                foreach (var pair in tempVariables)
                {
                    if (ContainsVariable(userINPUT, pair.Key))
                    {
                        var variableName = pair.Key;
                        string result = "";
                        bool add = true;
                        for (int i = 0; i < userINPUT.Length; ++i)
                        {
                            char c = userINPUT[i];
                            string currentVar = "";
                            result += c;
                            if (result.Length >= variableName.Length)
                            {
                                currentVar = result.Substring(i - variableName.Length + 1);
                            }
                            if (currentVar == variableName && (userINPUT.Length - result.Length == 0 || (userINPUT[i + 1]) == '=') && (i == variableName.Length - 1 || (userINPUT[i - variableName.Length]) == '='))
                            {
                                //The last x characters of result are a variable and the next character is an '=' sign
                                var value = BitCalculate(RemoveBrackets(ReplaceTempVariables(userINPUT.Substring(i + 2, userINPUT.Length - i - 2)), 'u'), 'u');
                                if (value != "null")
                                {
                                    toreplace.Add(variableName, value);
                                }
                                else
                                {
                                    tonull.Add(variableName);
                                }
                                mainresult = true;
                                add = false;
                                break;
                                //pair.Value = sINPUT.Substring(i + 1, sINPUT.Length - i - 1);
                            }
                        }
                        if (add)
                        {
                            toreplace.Add(variableName, tempVariables[variableName]);
                        }
                    }
                }
                //tempVariables = toreplace;
                Dictionary<string, string> dresult = new Dictionary<string, string>();
                foreach (var i in toreplace)
                {
                    dresult.Add(i.Key, i.Value);
                }
                foreach (var i in tempVariables)
                {
                    if (!dresult.ContainsKey(i.Key) && !tonull.Contains(i.Key))
                    {
                        dresult.Add(i.Key, i.Value);
                    }
                }
                tempVariables = dresult;
            }
            return mainresult;
        }
        public static Dictionary<string, string> tempVariables = new Dictionary<string, string>();
        public static Dictionary<string, Networking> networkingVariables = new Dictionary<string, Networking>();
        public static void DefineTempVariable(string variable)
        {
            if (variable.Contains("=new")) //Wants to open a port?
            {
                string name = variable.Split('=')[0];
                variable = variable.Substring(name.Length + 1 + 3); //+1 is to remove the equals. +3 is for the 'new'

                string[] variableData = variable.Split('_');
                ProtocolType protocolType;
                if (variableData[0] == "tcp") //Starting a TCP server?
                {
                    protocolType = ProtocolType.Tcp;
                }
                else if (variableData[0] == "udp")
                {
                    protocolType = ProtocolType.Udp;
                }
                else
                {
                    Program.expectingError = true;
                    throw new Exception("Invalid protocol type: " + variableData[0]);
                }

                string networkingType = variableData[1].Split('(')[0];
                if (networkingType == "client") //Creating a client?
                {
                    variable = variable.Substring(variable.NextBracket(0) + 1); //Remove everything up to the opening bracket
                    variable = variable.Substring(0, variable.Length - 1); //Remove final bracket
                    string[] data = variable.Split(',');
                    int port = int.Parse(data[0]);
                    string address = data[1];
                    if (address == "localhost")
                    {
                        address = "127.0.0.1";
                    }
                    try
                    {
                        IPAddress.Parse(address);
                    }
                    catch
                    {
                        Program.expectingError = true;
                        throw new Exception("Invalid IP address: " + address);
                    }
                    ClientNetworking clientNetworking = new ClientNetworking(address, port, CustomConsole.NetworkingPrint, protocolType);
                    networkingVariables.Add(name, clientNetworking);
                    Thread.Sleep(1000);
                }
                else if (networkingType == "server")
                {
                    variable = variable.Substring(variable.NextBracket(0) + 1); //Remove everything up to the opening bracket
                    variable = variable.Substring(0, variable.Length - 1); //Remove final bracket
                    int port = int.Parse(variable);
                    ServerNetworking serverNetworking = new ServerNetworking(port, CustomConsole.NetworkingPrint, protocolType);
                    networkingVariables.Add(name, serverNetworking);
                }
                else
                {
                    Program.expectingError = true;
                    throw new Exception("Invalid networking type: " + networkingType);
                }
                return;
            }

            string[] strings = variable.SplitAtFirst('=');
            int equalsIDX = strings[0].Length - 1;
            string value = strings[1];
            string variableName = variable.Substring(0, equalsIDX + 1);
            if (variableName.Any(c => !char.IsLetter(c) && c != ' '))
            {
                CustomConsole.PrintError("Invalid variable name");
                return;
            }
            if (VariableExists(variableName))
            {
                CustomConsole.PrintError("Variable is already defined");
                return;
            }
            tempVariables.Add(variableName, value);
        }
        public static string ReplaceTempVariables(string sINPUT)
        {
            foreach (var pair in networkingVariables)
            {
                if (sINPUT.StartsWith(pair.Key)) //Doing operation on a networking thingy?
                {
                    string operation = sINPUT.Split('.')[1]; //Find the function after the '.'
                    DoNetworkingOperation(pair.Value, operation);
                    return "CLOSE_CONDITION_PROCESSED";
                }
            }

            foreach (var pair in tempVariables)
            {
                sINPUT = ReplaceTempVariables(sINPUT, pair.Key, pair.Value);
            }
            return sINPUT;
        }
        /// <summary>
        /// Deletes the variable as the name specifies
        /// </summary>
        /// <param name="input"></param>
        private static void DeleteVariable(string input)
        {
            List<string> variables = File.ReadAllLines(DataFilePath).ToList(); //Find a list of all the variables
            var copy = new List<string>();
            foreach (var v in variables)
            {
                copy.Add(v);
            } //Copy the list so that we can iterate through this list while removing items from the other
            foreach (var s in variables)
            {
                var v = s.Split(',')[0];
                if (v == input) //Is this the variable we are deleting?
                {
                    copy.Remove(s); //Remove it from the list
                }
            }
            foreach (var v in copy)
            {
                PrintColour(v, true); //Print out the rest of the variables defined
            }
            File.WriteAllLines(DataFilePath, copy); //Write the new variable set to the file
        }
    }
}
