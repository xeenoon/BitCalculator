﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DevTools
{
    class Program
    {
        struct OperatorNum
        {
            public ulong _firstNum;
            public ulong _secondNum;
            public char _operator;

            public OperatorNum(ulong firstNum, ulong secondNum, char @operator)
            {
                _firstNum = firstNum;
                _secondNum = secondNum;
                _operator = @operator;
            }
        }
        public static ulong lastInput = 0ul;
        static bool defaultFlipVal = false;
        public static string lastprint;
        static void Main(string[] args)
        {
            CheckDirectories(); //See if the file storing directories exist, if not, then create them
            SetupConsole();

            bool first = true;
            while (true)
            {
                try
                {
                    Colorful.Console.Write("-->", Color.FromArgb(10, 181, 158)); //Header for text
                    string userInput = "";
                    if (args.Length != 0 && first) //Are we opening a file?
                    {
                        PrintColour("Opening file: " + args[0]); //Inform the user
                        var extension = args[0].Split('.')[1]; //Get the file extension from the filepath
                        if (extension != "dcode")
                        {
                            PrintColour("Unrecognized file extension: " + extension); //Only open .dcode files
                        }
                        else
                        {
                            string x = RemoveLineBreaks(File.ReadAllText(args[0])); //Read the file in
                            DoMainMethod(x); //Run the functions in the file
                        }
                        first = false; //This is no longer the first loop, set first to false
                    }
                    else
                    {
                        userInput = ReadLineOrEsc(); //Custom readline method to read text
                        ChangeUserTextColour(userInput); //Change colour of the header to show command has been processed
                        DoMainMethod(userInput); //Run users command
                    }
                }
                catch (Exception e)
                {
                    //Catch any error

                    Colorful.Console.WriteLine(e.Message, Color.FromArgb(255, 10, 10));
                    if (!expectingError) //IF this error was thrown externally of the application, show stack trace for debugging purposes (still in beta)
                    {
                        Colorful.Console.WriteLine(e.StackTrace, Color.FromArgb(255, 10, 10));
                    }
                    expectingError = false;
                }
            }
        }

        public static bool expectingError; //Set this to true, and it means the application is throwing a custom error
        //This will not print out a stack traces

        /// <summary>
        /// Checks to see if directories are valid. re-creates files if nessecary
        /// </summary>
        private static void CheckDirectories()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
            if (!File.Exists(DataFilePath))
            {
                File.CreateText(DataFilePath);
            }
            if (!File.Exists(WorkingsFilePath))
            {
                File.CreateText(WorkingsFilePath);
            }
            if (!File.Exists(FuncFilePath))
            {
                File.CreateText(FuncFilePath);
            }

            try
            {
                if (File.ReadAllText(WorkingsFilePath) == "")
                {
                    File.WriteAllText(WorkingsFilePath, printWorkings.ToString());
                }
                printWorkings = bool.Parse(File.ReadAllText(WorkingsFilePath));

                if (File.ReadAllText(FuncFilePath) == "")
                {
                    File.WriteAllText(FuncFilePath, Help.DEFAULTFUNCS);
                }
            }
            catch
            {
                Colorful.Console.WriteLine("DO NOT EDIT SYSTEM FILES. WORKINGS FILE HAS BEEN RESET");
            }
        }

        public static void DoMainMethod(string userINPUT, bool removeSpaces = true)
        {
            if (userINPUT.BeginsWith("help-")) //Show help for specific function. Specified after the -
            {
                PrintDescription(userINPUT.Substring(5)); //Print the help
                return;
            }
            if (userINPUT == "UNITTEST")
            {
                foreach (var test in UnitTest.unitTests)
                {
                    test.Test();
                }
                return;
            }
            if (!userINPUT.Contains("loop") && !userINPUT.Contains("#defunc"))
            //Only run multiple functions if function is not a loop or defining a function
            {
                foreach (var s in userINPUT.Split(';')) //Split up the different user commands
                {
                    MainMethod(s, removeSpaces); //Run the main method on them
                }
            }
            else
            {
                if (userINPUT.IndexOf(";") != 0 && userINPUT.IndexOf(";") < userINPUT.IndexOf("loop") && !userINPUT.Contains("#def"))
                //Run the functions that are called before the loop. After the loop statement, all semicolons are assumed to be inside it
                {
                    var beforeLoop = userINPUT.Substring(0, userINPUT.IndexOf("loop"));
                    foreach (var str in beforeLoop.Split(';'))
                    {
                        MainMethod(str, removeSpaces);
                    }
                    userINPUT = userINPUT.Substring(beforeLoop.Length); //Remove the previous statements from the userinputs
                }
                MainMethod(userINPUT, removeSpaces); //Run the mainmethod, with the changed length, or unmodified if there were no previous statements
            }
        }
        /// <summary>
        /// Write snazzy text in the console that is big
        /// </summary>
        /// <param name="text">text to print</param>
        static bool noprint = false;
        public static void MainMethod(string userINPUT, bool removeSpaces = true)
        {
            noprint = false;

            if (removeSpaces)
            {
                userINPUT = RemoveSpaces(userINPUT);
                userINPUT = RemoveComments(userINPUT);
            }
            #region uservariables
            if (userINPUT.BeginsWith("#define")) //Are we defining a variable?
            {
                DefineVariable(userINPUT); //Define the veriable with the users input
                return;
            }
            if (userINPUT.BeginsWith("#defunc"))
            {
                DefineFunction(userINPUT); //Define a function with the new input
                return;
            }
            if (userINPUT.BeginsWith("#delfunc"))
            {
                DeleteFunction(userINPUT.Substring(8)); //Delete the function
                return;
            }
            if (userINPUT.BeginsWith("#del"))
            {
                DeleteVariable(userINPUT.Substring(4)); //Delete the variable
                return;
            }
            var resetworkings = false;
            if (userINPUT.BeginsWith("nw")) //User wants to print with no workings?
            {
                printWorkings = false; //Stop printing workings
                userINPUT = userINPUT.Substring(2); //remove the "nw" from the userinput string
                if (!resetworkings)
                {
                    resetworkings = true; //Change the workings value back to normal when we are done
                }
            }

            userINPUT = RemoveX(userINPUT);
            if (userINPUT == "CLOSE_CONDITION_PROCESSED") //Boolean condition has already been processed. Exit the loop
            {
                return;
            }

            //Display/show variables
            if (userINPUT == "showfunc") //Show the user defined functions
            {
                PrintColour(File.ReadAllText(FuncFilePath));
                return;
            }
            if (userINPUT == "ipconfig") //Show the user defined functions
            {
                PrintIPData();
                return;
            }
            if (userINPUT.ToLower() == "dv") //Display all the user defined variables
            {
                foreach (var i in File.ReadAllLines(DataFilePath)) //Iterate through all the lines
                {
                    string copy = Regex.Replace(i, ",", " = "); //Replace the csv style commas with more user friendly " = "
                    PrintColour(copy, false); //Print the new variable
                }
                return;
            }
            if (userINPUT.ToLower() == "dtv") //Display all the user defined temp variables
            {
                foreach (var i in tempVariables) //Iterate through all the variables
                {
                    PrintColour(string.Format("{0} = {1}", i.Key, i.Value), false); //Print them to the screen
                }
                return;
            }
            #endregion


            if (userINPUT == "exit" || userINPUT == "quit") //close the app?
            {
                Environment.Exit(0);
            }
            if (userINPUT.StartsWith("alg")) //Generate algebra
            {
                userINPUT = userINPUT.Substring(4);
                userINPUT = userINPUT.Substring(0,userINPUT.Length-1); //Remove brackets
                int[] nums;
                try
                {
                    nums = userINPUT.Split(',').Select(s => int.Parse(s)).ToArray();
                }
                catch
                {
                    expectingError = true;
                    throw new Exception("Arguments must be numbers");
                }
                if (nums.Length != 3)
                {
                    expectingError = true;
                    throw new Exception("Expected 2 commas, recieved " + nums.Where(c=>c==',').Count());
                }
                FactoriseCrissCross(nums[0], nums[1], nums[2]);
                return;
            }

            string replaced = ReplaceTempVariables(userINPUT, 'v', lastInput.ToString()); //Define a new variable 'v' as the last result
            if (replaced != userINPUT) //Is the new value different to the old value. Used to stop infinite recursive loop
            {
                PrintColour(userINPUT + "-->" + replaced, true); //Show the user the change
                userINPUT = replaced; //Modify the user input to be the old input
            }


            if (userINPUT.BeginsWith("loop")) //User wants to do a loop?
            {
                DoLoopFunc(userINPUT); //Do the loop, then exit
                return;
            }
            #region showdecimals
            //Show value as decimal
            if (userINPUT.BeginsWith("doub")) //User wants to show value as double
            {
                PrintDouble(DoubleToBin(double.Parse(userINPUT.Substring(4)))); //Print the new double value
                double userDoubleInput = double.Parse(userINPUT.Substring(4));
                PrintColour("Closest conversion: " + userDoubleInput.ToString(), true); //Show the conversion
                string bitconv = Convert.ToString(BitConverter.DoubleToInt64Bits(userDoubleInput), 2);
                lastInput = Convert.ToUInt64(bitconv, 2); //Convert the double into a ulong to change last input
                return;
            }
            if (userINPUT.BeginsWith("float")) //User wants to show value as float
            {
                PrintFloat(FloatToBin(float.Parse(userINPUT.Substring(5)))); //Print the new float value
                float userFloatInput = float.Parse(userINPUT.Substring(5));
                PrintColour("Closest conversion: " + userFloatInput.ToString(), true); //Show the conversion
                string bitconv = Convert.ToString(BitConverter.SingleToInt32Bits(userFloatInput), 2);
                lastInput = Convert.ToUInt64(bitconv, 2); //Convert the double into a ulong to change last input
                return;
            }

            //Show previous bitset as decimal
            if (userINPUT == "adv") //Show previous bitset as a double
            {
                PrintDouble(DoubleToBin(BitConverter.Int64BitsToDouble((long)lastInput)));
                PrintColour("Double is: " + BitConverter.Int64BitsToDouble((long)lastInput), false);
                return;
            }
            if (userINPUT == "afv") //Show previous bitset as a float value
            {
                int lastinput__int = int.Parse(lastInput.ToString());
                float int32bits = BitConverter.ToSingle(BitConverter.GetBytes(lastinput__int));
                PrintFloat(FloatToBin(int32bits));
                PrintColour("Float is: " + BitConverter.Int32BitsToSingle(int.Parse(lastInput.ToString())), false);
                return;
            }
            #endregion

            if (userINPUT == "dt") //User wants to see the current date/time
            {
                PrintColour(DateTime.Now.ToString(), false); //Print the date/time
                return;
            }
            if (ModifyVariables(userINPUT)) //Is the user modifying variables that already exist
                                            //This function automatically deals with it, so we just need to finish
            {
                return;
            }
            if (userINPUT.BeginsWith("var"))
            {
                DefineTempVariable(userINPUT.Substring(3)); //Define a new temporary variable with the users input
                return;
            }
            if (userINPUT.BeginsWith("np")) //Does the user not want to print the binary value of the final result?
            {
                noprint = true; //Tell the binary printer NOT to print
                userINPUT = userINPUT.Substring(2); //Remove the string "np" from the userINPUT
            }
            if (userINPUT.BeginsWith("hrgb")) //Does the user want to convert a hex value into rgb
                                                                              //Returns rgb(255,255,255) for #ffffff
            {
                userINPUT = userINPUT.Substring(4); //Remove the "hrgb" from the calculation
                HEX_to_RGB(userINPUT); //Convert the hex value into rgb and print the result
                return;
            }
            if (userINPUT.BeginsWith("asci")) //Does the user want to draw ascii art
            {
                try
                {
                    userINPUT = RemoveBrackets(userINPUT, 'u');
                }
                catch
                {

                }
                WriteAscii(userINPUT.Substring(4, userINPUT.Length - 4)); //Remove the final bracket from the asci statement
                return;
            }
            if (userINPUT.BeginsWith("basci")) //Does the user want to draw snazzy binary ascii art
            {
                try
                {
                    userINPUT = RemoveBrackets(userINPUT, 'u');
                }
                catch
                {

                }

                BinaryNumASCI.PrintConverted(userINPUT.Substring(5, userINPUT.Length - 5)); //Remove the final bracket from the asci statement
                return;
            }
            if (userINPUT.ToLower() == "help") //Pretty damn well self explanatory
            {
                PrintHelp();
                return;
            }
            if (userINPUT.StartsWith("pw")) //Change the default value for printing workings or not
            {
                string value = userINPUT.Substring(3); //Remove start bracket
                value = value.Substring(0,value.Length-1); //Removing ending bracket
                printWorkings = bool.Parse(value);
                return;
            }
            if (userINPUT.ToLower() == "fpw") //Change the value for printing workings of not.. Write it to a file
            {
                string value = userINPUT.Substring(4); //Remove start bracket
                value = value.Substring(0, value.Length - 1); //Removing ending bracket
                printWorkings = bool.Parse(value);
                File.WriteAllText(WorkingsFilePath, printWorkings.ToString());
                return;
            }
            if (userINPUT.ToLower() == "cv") //Delete all variables
            {
                File.WriteAllText(DataFilePath, "");
                return;
            }

            if (userINPUT.BeginsWith("avg")) //User wants to get average number of a set
            {
                PrintColour("Average is: " + Average(userINPUT));
                return;
            }
            bool flipped = defaultFlipVal; //Flip binary output variable
            //Shows whether binary will be *left to right* or *right to left*

            if (userINPUT == "rf") //User wants to change the default flip value
            {
                defaultFlipVal = !defaultFlipVal;
                return;
            }
            bool is32bit = false;
            bool is16bit = false;
            bool is8bit = false;
            if (userINPUT.BeginsWith("ati")) //Weird math thingy. Description in the PrintAti() function
            {
                PrintAti(userINPUT);
                return;
            }
            if (userINPUT.BeginsWith("nslookup")) //User wants to find IP of a server
            {
                string addresses = "";
                foreach (var address in Dns.GetHostEntry(userINPUT.Substring(8)).AddressList)
                {
                    addresses += address;
                    addresses += ',';
                }
                addresses = addresses.Substring(0,addresses.Length-1); //Remove the final comma
                NetworkingPrint("Server IP: " + addresses);
                return;
            }
            if (userINPUT.BeginsWith("f")) //Flipping the binary result?
            {
                flipped = true; //Change the flipped value to true so that when we print binary later, we know what to do
                userINPUT = userINPUT.Substring(1); //Remove the 'f' from the string
                PrintColour("Printing flipped...", true); //Inform the user that the binary outcome is being flipped
            }

            if (userINPUT.BeginsWith("i")) //User wants to show binary value as 32i (32 bit uint)
            {
                is32bit = true; //Tell the binary printer to print only 32 bits
                userINPUT = userINPUT.Substring(1); //Remove the i from the thing being printed
            }
            else if (userINPUT.BeginsWith("s")) //User wants to show binary value as 16s (16 bit ushort)
            {
                is16bit = true; //Tell the binary printer to only print 16 bits
                userINPUT = userINPUT.Substring(1); //Remove the s from the thing being printed
            }
            else if (userINPUT.BeginsWith("b")) //User wants to show binary value as 8b (8 bit byte)
            {
                is8bit = true; //Tell the binary printer to only print 8 bits
                userINPUT = userINPUT.Substring(1); //Remove the b from the thing being printed
            }

            else if (userINPUT.BeginsWith("h")) //User wants to show as hexadecimal?
            {
                userINPUT = userINPUT.Substring(1); //Remove the h from the start
                PrintHex(ulong.Parse(userINPUT).ToString("X2").ToLower()); //Print the hex value of the users input
                return;
            }
            if (userINPUT.BeginsWith("#_")) //Converting hex value into ulong?
            {
                userINPUT = userINPUT.Substring(2);
                PrintColour(ulong.Parse(userINPUT, System.Globalization.NumberStyles.HexNumber).ToString(), false);
                //Convert from hex to ulong, and print the result
                return;
            }
            if (userINPUT.BeginsWith("doum")) //Check if the user wants to do doum math
            {
                userINPUT = userINPUT.Substring(4);
                userINPUT = DoubleCalculate(DoubleRemoveBrackets(userINPUT)); //Calculate the result

                //Print the value as a double
                if (!noprint)
                {
                    PrintDouble(DoubleToBin(double.Parse(userINPUT)));
                }
                PrintColour("Closest conversion: " + double.Parse(userINPUT).ToString());
                double d = double.Parse(userINPUT);
                string bitconv = Convert.ToString(BitConverter.DoubleToInt64Bits(d), 2);
                lastInput = Convert.ToUInt64(bitconv, 2);
                return;
            }

            char chosenType = 'u';
            if (is32bit)
            {
                chosenType = 'i';
                PrintColour("Printing as 32 bit int...");
            }
            else if (is16bit)
            {
                chosenType = 's';
                PrintColour("Printing as 16 bit short...");
            }
            else if (is8bit)
            {
                chosenType = 'b';
                PrintColour("Printing as 8 bit byte...");
            } //Change the chosen output type depending on the specified bit length
              //Default is 64 bit ulong

            userINPUT = RemoveBrackets(userINPUT, chosenType);
            string booleans = CheckForBooleans(userINPUT, chosenType); //Remove boolean conditions (==,!=,<,>)
            if (booleans == "true" || booleans == "false")
            {
                PrintColour(booleans, false); //Only do bool math, don't process following characters
                return;
            }
            if (BitCalculate(userINPUT, chosenType) != userINPUT)
            {
                userINPUT = BitCalculate(userINPUT, chosenType);
                if (!noprint)
                {
                    PrintColour(userINPUT); //Only print out the answer if there has been a calculation
                }
            }
            if (!ulong.TryParse(userINPUT, out ulong input))
            {
                expectingError = true;
                throw new Exception(string.Format("'{0}' is not a number", userINPUT));
            }
            if (!noprint) //Are we printing the binary values?
            {
                if (is32bit) //Print as 32 bit
                {
                    PrintColour(UlongToBin(input, flipped).Substring(66), false, true);
                }
                else if (is16bit) //Print as 16 bit
                {
                    PrintColour(UlongToBin(input, flipped).Substring(101), false, true);
                }
                else if (is8bit) //Print as 8 bit
                {
                    PrintColour(UlongToBin(input, flipped).Substring(118), false, true);
                }
                else //Print as ulong
                {
                    if (userINPUT == "") //Process blank
                    {
                        return;
                    }
                    PrintColour(UlongToBin(input, flipped), false, true);
                }
            }
            else
            {
                PrintColour(input.ToString());
            }
            lastInput = input; //Assign lastinput
            if (resetworkings) //Are we resetting the modified printworkings value
            {
                printWorkings = !printWorkings; //Reset static variable
            }
        }

        /// <summary>
        /// Weird math thingy
        /// </summary>
        /// <param name="userINPUT"></param>
        /// Gets all values for the different letters of the alphabet
        /// A=1
        /// B=2
        /// C=3
        /// .....
        /// Then multiplies them together
        /// AB = 2
        /// ABC = 6
        /// ABCD = 24
        /// CD = 12
        /// DB = 8
        /// etc....
        /// This is not designed to be a USEFUL tool, more just something to play around with
        static char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(); //Static string of all the letters in the alphabet
        private static void PrintAti(string userINPUT)
        {
            userINPUT = userINPUT.Substring(3);
            ulong total = 1;
            foreach (var c in userINPUT.ToUpper())
            {
                if (c == '(' || c== ')')
                {
                    continue; //Ignore all brackets
                }
                if (alphabet.ToList().IndexOf(c) == -1) //Not in alphabet? Continue to next element
                {
                    Colorful.Console.WriteLine(string.Format("Character: {0} not in alphabet. Disregarded in calculation", c)
                        , Color.FromArgb(255, 10, 10));
                    continue;
                }
                total *= (ulong)(alphabet.ToList().IndexOf(c) + 1); //Multiply equals by the next element
            }
            Colorful.Console.WriteAscii(string.Format("{0:n0}", total)); //Print result as snazzy asci text
        }
        public static string RemoveX(string userINPUT)
        {
            userINPUT = ReplaceTempVariables(userINPUT);
            userINPUT = RemoveHex(userINPUT);
            userINPUT = RemoveBinary(userINPUT);
            userINPUT = ReplaceVariables(userINPUT);
            userINPUT = RemoveTrig(userINPUT);
            userINPUT = RemoveLog(userINPUT);

            userINPUT = RemoveBooleanStatements(userINPUT);
            return userINPUT;
        }
        /// <summary>
        /// Runs a custom system function ran()
        /// Generates a random number between the first num, and the second num
        /// Looks for the position of ran( in the string, then looks for ) and splits the two numbers
        /// </summary>
        /// <param name="sINPUT"></param>
        /// <returns></returns>
        private static string RemoveRandom(string sINPUT)
        {
            if (sINPUT.Contains("ran(")) //Is the ran function in the users input
            {
                string buffer = "";
                for (int i = 0; i < sINPUT.Length; i++) //Iterate through the string
                {
                    char c = sINPUT[i]; //Using for instead of foreach to access the 'i' variable
                    buffer += c;
                    if (buffer.Contains("ran(")) //The moment the buffer contains ran
                                                 //i is the index of the (
                    {
                        int nextBracket = ClosingBracket(sINPUT, i + 1); //Find index of the closing brackets
                        string constraints = sINPUT.Substring(i + 1, nextBracket - i - 1); //Remove ran( and the closing brackets
                        //We are now left with two numbers and a comma

                        Random random = new Random();
                        string[] nums = constraints.Split(',');
                        nums[0] = RemoveBrackets(nums[0],'u');
                        nums[1] = RemoveBrackets(nums[1],'u'); //User may have variables or functions declared here. Check for these

                        int nextRan = random.Next(int.Parse(nums[0]), 1 + int.Parse(nums[1])); //+1 because max val is INCLUSIVE
                        PrintColour("Random number is: " + nextRan.ToString(), true);

                        //Rebuild the string
                        string before = sINPUT.Substring(0, i - 3); //Get the prev string value up until the ran(
                        string replace = nextRan.ToString(); //Replace the ran(x,y) with the random value
                        string after = sINPUT.Substring(nextBracket + 1); //Get the index of the trailing bracket
                        return RemoveRandom(before + replace + after);
                    }
                }
            }
            return sINPUT;
        }


        /// <summary>
        /// Removes boolean questions
        /// 4==4?3:2
        /// Replaces this entire statement with the new result
        /// 
        /// THIS CODE IS FULL OF BUGS
        /// REQUIRES FIXING
        /// </summary>
        /// <param name="sINPUT"></param>
        /// <returns></returns>
        private static string RemoveBooleanStatements(string sINPUT)
        {
            if (sINPUT.Contains('?') && !sINPUT.Contains(':')) //Only one condition and no else
            {
                for (int i = 0; i < sINPUT.Length; ++i)
                {
                    char c = sINPUT[i];
                    if (c == '?')
                    {
                        int lastOperatorIDX = LastNegOperatorIDX(sINPUT, i - 1);
                        string after = sINPUT.Substring(NextOperatorIDX_NoBrackets(sINPUT, i));
                        string inputCondition = sINPUT.Substring(lastOperatorIDX + 1, i - lastOperatorIDX - 1);

                        string conditionResult;
                        if (printWorkings == true)
                        {
                            printWorkings = false;
                            conditionResult = RemoveHex(RemoveBrackets(BitCalculate(CheckForBooleans(inputCondition, 'u'), 'u'), 'u'));
                            printWorkings = true;
                        }
                        else
                        {
                            conditionResult = RemoveHex(RemoveBrackets(BitCalculate(CheckForBooleans(inputCondition, 'u'), 'u'), 'u'));
                        }

                        PrintColour(String.Format("{0} is {1}", inputCondition, conditionResult), true);
                        if (conditionResult == "true")
                        {
                            string result = sINPUT.Substring(sINPUT.IndexOf('?') + 1, NextOperatorIDX_NoBrackets(sINPUT, i) - sINPUT.IndexOf('?') - 1); //Space between the ? and the : is the final condition
                            string before = sINPUT.Substring(0, LastOperatorIDX(sINPUT, sINPUT.IndexOfCondition() - 1) + 1);
                            if (sINPUT[NextOperatorIDX(sINPUT, 0)].IsConditionary()) //First operator is the boolean statement?
                            {
                                before = "";
                            }
                            DoMainMethod(before + result + after, false);
                        }
                        else
                        {
                            string result = "0";
                            string before = sINPUT.Substring(0, LastOperatorIDX(sINPUT, sINPUT.IndexOfCondition() - 1) + 1);
                            if (sINPUT[NextOperatorIDX(sINPUT, 0)].IsConditionary()) //First operator is the boolean statement?
                            {
                                before = "";
                            }
                            DoMainMethod(before + result + after, false);
                        }
                        return "CLOSE_CONDITION_PROCESSED";
                    }
                }
            }
            if (sINPUT.Contains('?') && sINPUT.Contains(':'))
            {
                for (int i = 0; i < sINPUT.Length; ++i)
                {
                    char c = sINPUT[i];
                    if (c == '?')
                    {
                        int lastOperatorIDX = LastNegOperatorIDX(sINPUT, i - 1);
                        int nextColonIDX = NextColonIDX(sINPUT, i + 1);
                        string after = sINPUT.Substring(NextOperatorIDX_NoBrackets(sINPUT, nextColonIDX));

                        string conditionResult = "";
                        string inputCondition = sINPUT.Substring(lastOperatorIDX + 1, i - lastOperatorIDX - 1);

                        if (printWorkings == true)
                        {
                            printWorkings = false;
                            conditionResult = RemoveHex(RemoveBrackets(BitCalculate(CheckForBooleans(inputCondition, 'u'), 'u'), 'u'));
                            printWorkings = true;
                        }
                        else
                        {
                            conditionResult = RemoveHex(RemoveBrackets(BitCalculate(CheckForBooleans(inputCondition, 'u'), 'u'), 'u'));
                        }
                        PrintColour(String.Format("{0} is {1}", inputCondition, conditionResult),true);


                        if (conditionResult == "true")
                        {
                            string result = sINPUT.Substring(sINPUT.IndexOf('?')+1, sINPUT.IndexOf(':')-sINPUT.IndexOf('?')-1); //Space between the ? and the : is the final condition
                            int lastoperatoridx = LastOperatorIDX(sINPUT, sINPUT.IndexOfCondition() - 1);
                            string before = sINPUT.Substring(0, lastOperatorIDX + 1);
                            if (sINPUT[NextOperatorIDX(sINPUT, 0)].IsConditionary()) //First operator is the boolean statement?
                            {
                                before = "";
                            }
                            DoMainMethod(before+result+after, false);
                        }
                        else
                        {
                            string result = sINPUT.Substring(sINPUT.IndexOf(':') + 1, NextOperatorIDX_NoBrackets(sINPUT, sINPUT.IndexOf(':')) - sINPUT.IndexOf(':') - 1); //Space between the ? and the : is the final condition
                            string before = sINPUT.Substring(0, LastOperatorIDX(sINPUT, sINPUT.IndexOfCondition() - 1) + 1);
                            if (sINPUT[NextOperatorIDX(sINPUT, 0)].IsConditionary()) //First operator is the boolean statement?
                            {
                                before = "";
                            }
                            DoMainMethod(before + result + after, false);
                        }
                        return "CLOSE_CONDITION_PROCESSED";
                    }
                }
            }
            return sINPUT;
        }

        /// <summary>
        /// Replaces binary as defined by b_010101  with its corresponding int value
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string RemoveBinary(string input)
        {
            char prev = ' ';
            if (input.Contains("b_")) //Is there binary to remove?
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char c = (char)input[i];
                    if (prev == 'b' && c == '_') //Are we at the start of the binary??
                    {
                        string fixedval = input.Substring(0, i - 1); //The statement that came previously to the binary num

                        int nextOperaror = NextOperatorIDX_NoLetter(input, i + 1); //Find the index of the next operator so that we know when the binary statement ends
                        string binNum = Convert.ToUInt64(input.Substring(i + 1, nextOperaror - i - 1), 2).ToString(); //Find the binary num, convert it to a uint64

                        string afterThat = input.Substring(nextOperaror, input.Length - nextOperaror); //Find the trailing characters
                        PrintColour(string.Format("{0} --> {1}", input, fixedval + binNum + afterThat), true); //Show the user what has been replaced
                        return RemoveBinary(fixedval + binNum + afterThat); //There may be more binary to find, so look for that
                    }
                    prev = c;
                }
            }
            return input;
        }
        /// <summary>
        /// Removes hex from the users input
        /// Comes in the form #ffffff
        /// So far this works, so eh, not gonna document it for now
        /// Will proabbly not be involved in the new features being created
        /// 
        /// 'PROBABLY'
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string RemoveHex(string input)
        {
            if (input.Length >= 2 && input[1] == '_')
            {
                return input;
            }
            if (input.Contains('#'))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char c = (char)input[i];
                    if (c == '#')
                    {
                        string fixedval = input.Substring(0, i);
                        int nextOperaror = NextOperatorIDX_NoLetter(input, i + 1);
                        string hexNum = ulong.Parse(input.Substring(i + 1, nextOperaror - i - 1), System.Globalization.NumberStyles.HexNumber).ToString();
                        string afterThat = input.Substring(nextOperaror, input.Length - nextOperaror);
                        if (printWorkings)
                        {
                            PrintColour(input.Substring(i + 1, nextOperaror - i - 1) + " --> " + hexNum);
                        }
                        return RemoveHex(fixedval + hexNum + afterThat);
                    }
                }
            }
            return input;
        }
        /// <summary>
        /// Converts hex to rgb
        /// You type in hrgb #ffffff
        /// An voila, it converts it into rgb(255,255,255)
        /// NOT to be used in conjunction with other things
        /// 
        /// So far this works, so eh, cant be bothered documenting it
        /// </summary>
        /// <param name="hexVal"></param>
        public static void HEX_to_RGB(string hexVal)
        {
            if (hexVal[0] == '#')
            {
                hexVal = hexVal.Substring(1);
            }
            string result = "rgb(";
            string buffer = "";
            for (int i = 0; i < hexVal.Length; ++i)
            {
                buffer += hexVal[i];
                if (buffer.Length == 2)
                {
                    result += ulong.Parse(buffer, System.Globalization.NumberStyles.HexNumber).ToString();
                    result += ',';
                    buffer = "";
                }
            }
            result = result.Substring(0, result.Length - 1);
            result += ");";
            PrintColour(result, false);
        }
        public static void DoLoopFunc(string loop)
        {
            loop = loop.Substring(4);
            string tocalc = loop.Split(':')[0];
            string s = BitCalculate(RemoveBrackets(tocalc, 'u'), 'u');
            int timesAround = int.Parse(s);
            loop = loop.Substring(tocalc.Length + 1);
            for (int i = 0; i < timesAround; ++i)
            {
                string currentLoop = ReplaceTempVariables(loop, 'i', i.ToString());
                DoMainMethod(currentLoop);
            }
        }

        public static void DoNetworkingOperation(Networking n, string operation)
        {
            if (operation.BeginsWith("send")) //User wants to send data?
            {
                string data = operation.Substring(5);
                data = data.Substring(0,data.Length-1); //Remove send( and )
                n.Send(data);
            }
        }

        public static string DoubleToBin(double input, bool flipped = false)
        {
            string binVal = Convert.ToString(BitConverter.DoubleToInt64Bits(input), 2);
            string result = "";
            for (int i = 0; i < 64-binVal.Length; ++i)
            {
                result += '0';
            }
            result += binVal;
            string[] thirdResult = new string[8];
            int currIDX = 0;
            for (int i = 0; i < 64; ++i)
            {
                if (i % 8 == 0 && i != 0)
                {
                    ++currIDX;
                }
                thirdResult[currIDX] += result[i];
                thirdResult[currIDX] += ' ';
            }
            string finalResult = "";
            for (int i = 0; i < thirdResult.Length; ++i)
            {
                if (flipped)
                {
                    finalResult += new string(thirdResult[i].Reverse().ToArray());
                }
                else
                {
                    finalResult += thirdResult[i];
                }
                finalResult += '\n';
            }
            return finalResult;
        }
        public static string FloatToBin(float input, bool flipped = false)
        {
            string binVal = Convert.ToString(BitConverter.SingleToInt32Bits(input), 2);
            string result = "";
            for (int i = 0; i < 32 - binVal.Length; ++i)
            {
                result += '0';
            }
            result += binVal;
            string[] thirdResult = new string[8];
            int currIDX = 0;
            for (int i = 0; i < 32; ++i)
            {
                if (i % 8 == 0 && i != 0)
                {
                    ++currIDX;
                }
                thirdResult[currIDX] += result[i];
                thirdResult[currIDX] += ' ';
            }
            string finalResult = "";
            for (int i = 0; i < thirdResult.Length; ++i)
            {
                if (flipped)
                {
                    finalResult += new string(thirdResult[i].Reverse().ToArray());
                }
                else
                {
                    finalResult += thirdResult[i];
                }
                finalResult += '\n';
            }
            return finalResult;
        }
        private static void PrintHelp()
        {
            WriteHelp("Welcome to DevTools 2022");
            WriteHelp("Below listed are the available functions you can use");
            WriteHelp("To get data on how to use the function, just type *help-functionname*");
            Console.WriteLine();
            PrintColour("loop");
            PrintColour("#define");
            PrintColour("#defunc");
            PrintColour("#delfunc");
            PrintColour("#del");
            PrintColour("nw");
            PrintColour("showfunc");
            PrintColour("dv");
            PrintColour("dtv");
            PrintColour("exit");
            PrintColour("quit");
            PrintColour("ran");
            PrintColour("alg");
            PrintColour("v");
            PrintColour("doub");
            PrintColour("float");
            PrintColour("adv");
            PrintColour("afv");
            PrintColour("dt");
            PrintColour("var");
            PrintColour("np");
            PrintColour("hrgb");
            PrintColour("asci");
            PrintColour("basci");
            PrintColour("pw");
            PrintColour("fpw");
            PrintColour("cv");
            PrintColour("avg");
            PrintColour("r");
            PrintColour("rf");
            PrintColour("ati");
            PrintColour("i");
            PrintColour("s");
            PrintColour("b");
            PrintColour("h");
            PrintColour("#_");
            PrintColour("b_");
            PrintColour("doum");
            PrintColour("booleans");
            PrintColour("bitmath");
            PrintColour("trig");
            PrintColour("log");
            PrintColour("ipconfig");
            PrintColour("open");
            PrintColour("tcp_client");
            PrintColour("tcp_server");
            PrintColour("udp_client");
            PrintColour("udp_server");
            PrintColour("nslookup");
            PrintColour("");
            WriteHelp("You can also type in math equations using math operators *,/,+,-");
        }
        public static void WriteHelp(string s)
        {
            ShowDescription(s.Insert(0,@"///") + @"\\\");
        }
        static string DataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\DevTools";

        static string DataFile = @"\data.txt";
        public static string DataFilePath = DataDirectory + DataFile;
                      
        static string WorkingsFile = @"\workings.txt";
        public static string WorkingsFilePath = DataDirectory + WorkingsFile;

        static string funcsFile = @"\funcs.txt";
        public static string FuncFilePath = DataDirectory + funcsFile;
        
        public static string ReplaceVariables(string input)
        {

            string i = input;
            foreach (var s in DefineVariableContents())
            {
                if (!s.Contains(','))
                {
                    File.WriteAllText(DataFilePath, "");
                    PrintColour("All variables cleared because of invalid input. DO NOT EDIT THE VARIABLES FILE", false);
                    return "";
                }

                var ss = s.SplitAtFirst(',');
                i = Regex.Replace(i, ss[0], "(" + ss[1] + ")");
            }
            foreach (var s in File.ReadAllLines(FuncFilePath))
            {
                if (s == "")
                {
                    continue;
                }
                if (s == "SYSTEM FUNCTIONS:")
                {
                    break;
                }
                if (!s.Contains('('))
                {
                    File.WriteAllText(FuncFilePath, Help.DEFAULTFUNCS);
                    PrintColour("All FUNCTIONS cleared because of invalid input. DO NOT EDIT THE functions FILE", false);
                    return "";
                }
                var name = s.Split('(')[0];
                if (i.Contains(name))
                {
                    string replacestring = s;
                    int closingBracketidx = ClosingBracket(replacestring, name.Length + 1);
                    replacestring = replacestring.Substring(closingBracketidx + 1);

                    int valuesstartidx = i.IndexOf(name) + name.Length + 1;
                    string[] values = i.Substring(valuesstartidx, ClosingBracket(i, valuesstartidx) - valuesstartidx).Split(',');
                    string[] names = s.Substring(name.Length + 1, ClosingBracket(s, name.Length + 1) - name.Length - 1).Split(',');
                    Dictionary<string, int> variableValues = new Dictionary<string, int>();

                    if (values.Length != names.Length)
                    {
                        expectingError = true;
                        throw new Exception(string.Format("Recieved {0} arguments, expected {1}", values.Length, names.Length));
                    }

                    //Iterate through here and add the variable values to the variable names
                    //swap out the variable values for the variable names in the function stored file
                    //Replace the function text with the text found in the file

                    for (int idx = 0; idx < values.Length; ++idx)
                    {
                        replacestring = ReplaceTempVariables(replacestring, names[idx], values[idx]);
                    }
                    if (replacestring.Contains("///"))
                    {
                        replacestring = replacestring.Substring(0, replacestring.IndexOf("///"));
                    }
                    string before = i.Substring(0, i.IndexOf(name));
                    string after = i.Substring(ClosingBracket(i, i.IndexOf(name) + name.Length + 1) + 1);
                    i = before + replacestring + after;
                    return ReplaceVariables(i);
                }
            }
            if (i != input)
            {
                PrintColour(i, true);
            }
            i = RemoveRandom(i);
            return i;
        }
        public static string RemoveAndReplace(int startIDX, int endIDX, string replaceWith, string input)
        {
            return input.Substring(0, startIDX) + replaceWith + input.Substring(endIDX, input.Length - endIDX);
        }
        public static string TextBetween(string input, int startIDX, int endIDX)
        {
            string result = "";
            if (endIDX == input.Length)
            {
                endIDX -= 1;
            }
            for (int i = startIDX; i < endIDX + 1; ++i)
            {
                result += input[i];
            }
            return result;
        }
        /// <summary>
        /// Looks for boolean statements: ==,!=,>,<. Processes their values
        /// RETURNED VALUE IS A STRING. DO NOT PROCESS AS BOOL
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string CheckForBooleans(string input, char type)
        {
            if (StringContains(input, '<'))
            {
                var strings = SplitAt(input, '<');
                strings[0] = BitCalculate(strings[0], type);
                strings[1] = BitCalculate(strings[1], type);
                if (ulong.Parse(strings[0])<ulong.Parse(strings[1]))
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            if (StringContains(input, '>'))
            {
                var strings = SplitAt(input, '>');
                strings[0] = BitCalculate(strings[0], type);
                strings[1] = BitCalculate(strings[1], type);
                if (ulong.Parse(strings[0]) > ulong.Parse(strings[1]))
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            if (input.Contains("=="))
            {
                string[] strings=  input.Split(new string[] { "==" }, StringSplitOptions.None); 
                strings[0] = BitCalculate(strings[0], type);
                strings[1] = BitCalculate(strings[1], type);
                if (ulong.Parse(strings[0]) == ulong.Parse(strings[1]))
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            if (input.Contains("!="))
            {
                string[] strings = input.Split(new string[] { "!=" }, StringSplitOptions.None);
                strings[0] = BitCalculate(strings[0], type);
                strings[1] = BitCalculate(strings[1], type);
                if (ulong.Parse(strings[0]) != ulong.Parse(strings[1]))
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            return input;
        }
        private static List<string> SplitAt(string input, char v)
        {
            List<string> result = new List<string>();
            string buffer = "";
            bool nextCantBeV = false;
            foreach (var c in input)
            {
                if (c == v && nextCantBeV)
                {
                    nextCantBeV = false;
                    buffer += c;
                    continue;
                }
                if (nextCantBeV)
                {
                    result.Add(RemoveLast(buffer));
                    buffer = "";
                    nextCantBeV = false;
                    buffer += c;
                    continue;
                }
                if (c == v && buffer.Length != 0 && buffer[buffer.Length - 1] != v)
                {
                    nextCantBeV = true;
                }
                buffer += c;
            }
            result.Add(buffer);
            return result;
        }

        public static string UlongToBin(ulong input, bool flipped)
        {
            string firstResult = "";
            while (input >= 1)
            {
                ulong remainder = input % 2;
                firstResult = remainder + firstResult;
                input /= 2;
            }
            string secondResult = "";
            for (int i = 64 - firstResult.Length; i > 0; --i)
            {
                secondResult += "0";
            }
            secondResult += firstResult;
            string[] thirdResult = new string[8];
            int currIDX = 0;
            for (int i = 0; i < 64; ++i)
            {
                if (i % 8 == 0 && i != 0)
                {
                    ++currIDX;
                }
                thirdResult[currIDX] += secondResult[i];
                thirdResult[currIDX] += ' ';
            }
            string finalResult = "";
            for (int i = 0; i < thirdResult.Length; ++i)
            {
                if (flipped)
                {
                    finalResult += new string(thirdResult[i].Reverse().ToArray());
                }
                else
                {
                    finalResult += thirdResult[i];
                }
                finalResult += '\n';
            }
            return finalResult;
        }
    }
}