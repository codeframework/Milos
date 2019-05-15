using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

namespace Milos.Core.Utilities
{
    /// <summary>
    /// This class provides a number of (static) methods that are useful when working with strings.
    /// Some of these methods have been migrated from the VFPToolkit class written by Kamal Patel.
    /// Special thanks go to Kamal. (www.KamalPatel.com)
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Returns a culture-neutral to-lower operation on the string.
        /// </summary>
        /// <param name="originalString">Original string</param>
        /// <returns>Lower-case string</returns>
        public static string Lower(string originalString) => originalString.ToLower(CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns a culture-neutral to-upper operation on the string.
        /// </summary>
        /// <param name="originalString">Original string</param>
        /// <returns>Upper-case string</returns>
        public static string Upper(string originalString) => originalString.ToUpper(CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns the string in a culture-neutral fashion
        /// </summary>
        /// <param name="value">Value to be turned into a string</param>
        /// <returns>String</returns>
        public static string ToString(object value) => value is IFormattable formatTableValue ? formatTableValue.ToString(null, CultureInfo.InvariantCulture) : value.ToString();

        /// <summary>
        /// Returns true if the two strings match.
        /// </summary>
        /// <param name="firstString">First string</param>
        /// <param name="secondString">Second string</param>
        /// <returns>True or False</returns>
        /// <remarks>
        /// The strings are trimmed and compared in a case-insensitive, culture neutral fashion.
        /// </remarks>
        public static bool Compare(string firstString, string secondString) => string.Compare(firstString.Trim(), secondString.Trim(), true, CultureInfo.InvariantCulture) == 0;

        /// <summary>
        /// Returns true if the two strings match.
        /// </summary>
        /// <param name="firstString">First string</param>
        /// <param name="secondString">Second string</param>
        /// <param name="ignoreCase">Should case (upper/lower) be ignored?</param>
        /// <returns>True or False</returns>
        /// <remarks>
        /// The strings are trimmed and compared in a case-insensitive, culture neutral fashion.
        /// </remarks>
        public static bool Compare(string firstString, string secondString, bool ignoreCase) => string.Compare(firstString.Trim(), secondString.Trim(), ignoreCase, CultureInfo.InvariantCulture) == 0;

        ///// <summary>
        ///// Receives a string as a parameter and returns the string in Proper format (makes each letter after a space capital)
        ///// <pre>
        ///// Example:
        ///// StringHelper.Proper("joe doe is a good man");	//returns "Joe Doe Is A Good Man"
        ///// </pre>
        ///// </summary>
        ///// <param name="originalString">String</param>
        ///// <returns>Proper string</returns>
        //public static string Proper(string originalString)
        //{
        //    // TODO: This method is rather fishy!

        //    //Create the StringBuilder
        //    var sb = new StringBuilder(originalString);
        //    var length = originalString.Length;

        //    for (var counter = 0; counter < length; counter++)
        //        //look for a blank space and once found make the next character to uppercase
        //        if ((counter == 0) || (char.IsWhiteSpace(originalString[counter])))
        //        {

        //            int counter2;
        //            //Handle the first character differently
        //            if (counter == 0) { counter2 = counter; }
        //            else { _ = counter + 1; }

        //            //Make the next character uppercase and update the stringBuilder
        //            sb.Remove(0, 1);
        //            sb.Insert(0, char.ToUpper(originalString[0], CultureInfo.InvariantCulture));
        //        }
        //    return sb.ToString();
        //}

        ///// <summary>
        ///// This method returns strings in proper case.
        ///// However, contrary to regular Proper() methods, 
        ///// this method can be used to format names.
        ///// For instance, "MacLeod" will remain "MacLeod",
        ///// "macLeod" will be "MacLeod", "MACLEOD" will be turned into
        ///// "Macleod". "macleod" will also be turned into "Macleod".
        ///// </summary>
        ///// <param name="originalString">String that is to be formatted</param>
        ///// <returns>Properly formatted string</returns>
        //public static string SmartProper(string originalString)
        //{
        //    // TODO: This method is rather fishy!
        //    var chars = originalString.Trim().ToCharArray();

        //    var dummy = string.Empty;
        //    var sbWord = new StringBuilder();
        //    var lastWasNewWord = true;			// Indicated that the last character started a new word
        //    var encounteredLower = false;
        //    var encounteredUpper = false;

        //    for (var counter = 0; counter < chars.Length; counter++)
        //    {
        //        dummy = chars[counter].ToString();

        //        // We figure out whether this was a lower or upper case character
        //        encounteredLower = dummy.ToLower(CultureInfo.InvariantCulture) == dummy;
        //        encounteredUpper = dummy.ToUpper(CultureInfo.InvariantCulture) == dummy;

        //        if (lastWasNewWord)
        //            // Ever time we start a new word, the first char is upper case, no matter what.
        //            sbWord.Append(dummy.ToUpper(CultureInfo.InvariantCulture));
        //        else
        //        {
        //            // We are in the middle of a word. We may have to lower chars, unless the word was in camel case before
        //            if (encounteredUpper && encounteredLower)
        //                // We have a camel chase word. We do not change anything
        //                sbWord.Append(dummy);
        //            else
        //                sbWord.Append(dummy.ToLower(CultureInfo.InvariantCulture));
        //        }

        //        // We check whether the current char starts a new word.
        //        lastWasNewWord = (dummy == " " || dummy == "-" || dummy == "'" || dummy == "." || dummy == "," || dummy == ";" || dummy == ":") ? true : false;
        //        if (lastWasNewWord)
        //            encounteredUpper = false;
        //    }

        //    return sbWord.ToString();
        //}

        /// <summary>
        /// This method takes a camel-case string (such as one defined by an enum)
        /// and returns is with a space before every upper-case letter.
        /// Example: "CamelCaseWord" turns into "Camel Case Word"
        /// </summary>
        /// <param name="originalString">String</param>
        /// <returns>String with spaces</returns>
        public static string SpaceCamelCase(string originalString)
        {
            var chars = originalString.Trim().ToCharArray();
            var sbWord = new StringBuilder();
            for (var counter = 0; counter < chars.Length; counter++)
            {
                var dummy = chars[counter].ToString();
                if (counter > 0 && dummy.ToUpper(CultureInfo.InvariantCulture) == dummy)
                    sbWord.Append(" ");
                sbWord.Append(dummy);
            }

            return sbWord.ToString();
        }

        /// <summary>
        /// Receives a string and a file name as parameters and writes the contents of the
        /// string to that file
        /// <pre>
        /// Example:
        /// string sString = "This is the line we want to insert in our file.";
        /// StringHelper.ToFile(sString, "c:\\My Folders\\MyFile.txt");
        /// </pre>
        /// </summary>
        /// <param name="expression">String to be written</param>
        /// <param name="fileName">File name the string is to be written to.</param>
        /// <param name="encoding">File encoding</param>
        public static void ToFile(string expression, string fileName, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.Default;

            if (File.Exists(fileName))
                //If so then Erase the file first as in this case we are overwriting
                File.Delete(fileName);

            using (var fileStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
            using (var fileWriter = new StreamWriter(fileStream, encoding))
            {
                fileWriter.Write(expression);
                fileWriter.Flush();
                fileWriter.Close();
                fileStream.Close();
            }
        }

        /// <summary>
        /// Loads a file from disk and returns it as a string
        /// </summary>
        /// <param name="fileName">File to be loaded</param>
        /// <returns>String containing the file contents</returns>
        public static string FromFile(string fileName)
        {
            using (var fileReader = File.OpenText(fileName))
            {
                var returnValue = fileReader.ReadToEnd();
                fileReader.Close();
                return returnValue;
            }
        }

        /// <summary>
        /// This method takes any regular string, and returns its base64 encoded representation
        /// </summary>
        /// <param name="original">Original String</param>
        /// <returns>Base64 encoded string</returns>
        public static string Base64Encode(string original)
        {
            return Convert.ToBase64String(new ASCIIEncoding().GetBytes(original));
        }

        /// <summary>
        /// Takes a base64 encoded string and converts it into a regular string
        /// </summary>
        /// <param name="encodedString">Base64 encoded string</param>
        /// <returns>Decoded string</returns>
        public static string Base64Decode(string encodedString)
        {
            return new ASCIIEncoding().GetString(Convert.FromBase64String(encodedString));
        }

        /// <summary>
        /// Receives two strings as parameters and searches for one string within another. 
        /// If found, returns the beginning numeric position otherwise returns 0
        /// <pre>
        /// Example:
        /// StringHelper.At("D", "Joe Doe");	//returns 5
        /// </pre>
        /// </summary>
        /// <param name="searchFor">String to search for</param>
        /// <param name="searchIn">String to search in</param>
        /// <returns>Position</returns>
        public static int At(string searchFor, string searchIn) => searchIn.IndexOf(searchFor, StringComparison.Ordinal) + 1;

        /// <summary>
        /// Receives two strings and an occurrence position (1st, 2nd etc) as parameters and 
        /// searches for one string within another for that position. 
        /// If found, returns the beginning numeric position otherwise returns 0
        /// <pre>
        /// Example:
        /// StringHelper.At("o", "Joe Doe", 1);	//returns 2
        /// StringHelper.At("o", "Joe Doe", 2);	//returns 6
        /// </pre>
        /// </summary>
        /// <param name="searchFor">String to search for</param>
        /// <param name="searchIn">String to search in</param>
        /// <param name="occurrence">The occurrence of the string</param>
        /// <returns>Position</returns>
        public static int At(string searchFor, string searchIn, int occurrence)
        {
            // TODO: This is fishy
            return __at(searchFor, searchIn, occurrence, 1);
        }

        /// <summary>
        /// Private Implementation: This is the actual implementation of the At() and RAt() functions. 
        /// Receives two strings, the expression in which search is performed and the expression to search for. 
        /// Also receives an occurrence position and the mode (1 or 0) that specifies whether it is a search
        /// from Left to Right (for At() function)  or from Right to Left (for RAt() function)
        /// </summary>
        /// <param name="searchFor">String to search for</param>
        /// <param name="searchIn">String to search in</param>
        /// <param name="occurrence">occurrence of the string</param>
        /// <param name="mode">Mode</param>
        /// <returns>Position</returns>
        private static int __at(string searchFor, string searchIn, int occurrence, int mode)
        {
            // TODO: This is fishy
            //In this case we actually have to locate the occurrence
            var counter = 0;
            var occured = 0;
            var position = 0;
            if (mode == 1)
                position = 0;
            else
                position = searchIn.Length;

            //Loop through the string and get the position of the requiref occurrence
            for (counter = 1; counter <= occurrence; counter++)
            {
                if (mode == 1)
                    position = searchIn.IndexOf(searchFor, position, StringComparison.Ordinal);
                else
                    position = searchIn.LastIndexOf(searchFor, position, StringComparison.Ordinal);

                if (position < 0)
                    //This means that we did not find the item
                    break;

                //Increment the occured counter based on the current mode we are in
                occured++;

                //Check if this is the occurrence we are looking for
                if (occured == occurrence) return position + 1;

                if (mode == 1)
                    position++;
                else
                    position--;
            }

            //We never found our guy if we reached here
            return 0;
        }

        /// <summary>
        /// Receives a character as a parameter and returns its ANSI code
        /// <pre>
        /// Example
        /// Asc('#');		//returns 35
        /// </pre>
        /// </summary>
        /// <param name="character">Character</param>
        /// <returns>ASCII value</returns>
        public static int Asc(char character) => character;

        /// <summary>
        /// Receives an integer ANSI code and returns a character associated with it
        /// <pre>
        /// Example:
        /// StringHelper.Chr(35);		//returns '#'
        /// </pre>
        /// </summary>
        /// <param name="ansiCode">Character Code</param>
        /// <returns>Char that corresponds with the ascii code</returns>
        public static char Chr(int ansiCode) => (char) ansiCode;

        /// <summary>
        /// Receives a string as a parameter and counts the number of words in that string
        /// <pre>
        /// Example:
        /// string lcString = "Joe Doe is a good man";
        /// StringHelper.GetWordCount(lcString);		//returns 6
        /// </pre>
        /// </summary>
        /// <param name="sourceString">String</param>
        /// <returns>Word Count</returns>
        public static int GetWordCount(string sourceString)
        {
            var wordCount = 0;

            //Begin by checking for the first word
            if (!char.IsWhiteSpace(sourceString[0])) wordCount++;

            //Now look for white spaces and count each word
            for (var counter = 0; counter < sourceString.Length; counter++)
                //Check for a space to begin counting a word
                if (char.IsWhiteSpace(sourceString[counter]))
                    //We think we encountered a word
                    //Remove any following white spaces if any after this word
                    do
                    {
                        //Check if we have reached the limit and if so then exit the loop
                        counter++;
                        if (counter >= sourceString.Length) break;
                        if (!char.IsWhiteSpace(sourceString[counter]))
                        {
                            wordCount++;
                            break;
                        }
                    } while (true);

            return wordCount;
        }

        /// <summary>
        /// Based on the position specified, returns a word from a string 
        /// Receives a string as a parameter and counts the number of words in that string
        /// <pre>
        /// Example:
        /// string lcString = "Joe Doe is a good man";
        /// StringHelper.GetWordNumber(lcString, 5);		//returns "good"
        /// </pre>
        /// </summary>
        /// <param name="sourceString">String</param>
        /// <param name="wordPosition">Word Position</param>
        /// <returns>Word number</returns>
        public static string GetWordNumb(string sourceString, int wordPosition)
        {
            if (wordPosition < 1) return string.Empty;

            var words = sourceString.Split(' ');
            return wordPosition <= words.Length ? words[wordPosition - 1] : string.Empty;
        }

        /// <summary>
        /// Returns a bool indicating if the first character in a string is an alphabet or not
        /// <pre>
        /// Example:
        /// StringHelper.IsAlpha("Joe Doe");		//returns true
        /// 
        /// Tip: This method uses Char.IsAlpha(char) to check if it is an alphabet or not. 
        ///      In order to check if the first character is a digit use Char.IsDigit(char)
        /// </pre>
        /// </summary>
        /// <param name="expression">Expression</param>
        /// <returns>True or False depending on whether the string only had alphanumeric chars</returns>
        public static bool IsAlpha(string expression) => char.IsLetter(expression[0]);

        /// <summary>
        /// Returns the number of occurrences of a character within a string
        /// <pre>
        /// Example:
        /// StringHelper.Occurs('o', "Joe Doe");		//returns 2
        /// 
        /// Tip: If we have a string say lcString, then lcString[3] gives us the 3rd character in the string
        /// </pre>
        /// </summary>
        /// <param name="character">Search Character</param>
        /// <param name="expression">Expression</param>
        /// <returns>Number of occurrences</returns>
        public static int Occurs(char character, string expression)
        {
            var occured = 0;

            for (var counter = 0; counter < expression.Length; counter++)
                if (expression[counter] == character)
                    occured++;

            return occured;
        }

        /// <summary>
        /// Returns the number of occurrences of one string within another string
        /// <pre>
        /// Example:
        /// StringHelper.Occurs("oe", "Joe Doe");		//returns 2
        /// StringHelper.Occurs("Joe", "Joe Doe");		//returns 1
        /// 
        /// Tip: String.IndexOf() searches the string (starting from left) for another character or string expression
        /// </pre>
        /// </summary>
        /// <param name="searchString">Search String</param>
        /// <param name="stringSearched">Expression</param>
        /// <returns>Number of occurrences</returns>
        public static int Occurs(string searchString, string stringSearched)
        {
            var position = 0;
            var occured = 0;
            do
            {
                //Look for the search string in the expression
                position = stringSearched.IndexOf(searchString, position, StringComparison.Ordinal);

                if (position < 0)
                    //This means that we did not find the item
                    break;

                //Increment the occured counter based on the current mode we are in
                occured++;
                position++;
            } while (true);

            //Return the number of occurrences
            return occured;
        }

        /// <summary>
        /// Receives a string expression and a numeric value indicating number of time
        /// and replicates that string for the specified number of times.
        /// <pre>
        /// Example:
        /// StringHelper.Replicate("Joe", 5);		//returns JoeJoeJoeJoeJoe
        /// 
        /// Tip: Use a StringBuilder when lengthy string manipulations are required.
        /// </pre>
        /// </summary>
        /// <param name="expression">Expression</param>
        /// <param name="times">Number of times the string is to be replicated</param>
        /// <returns>New string</returns>
        public static string Replicate(string expression, int times) => new StringBuilder().Insert(0, expression, times).ToString();

        /// <summary>
        /// Overloaded method for SubStr() that receives starting position and length
        /// </summary>
        /// <param name="expression">Expression</param>
        /// <param name="startPosition">Start Position</param>
        /// <param name="length">Length</param>
        /// <returns>Substring</returns>
        public static string SubStr(string expression, int startPosition, int length)
        {
            if (startPosition >= expression.Length) return string.Empty;
            return length + startPosition - 1 > expression.Length ? expression.Substring(startPosition - 1) : expression.Substring(startPosition - 1, length);
        }

        /// <summary>
        /// Receives a string and converts it to an integer
        /// <pre>
        /// Example:
        /// StringHelper.AtLine("Is", "Is Life Beautiful? \r\n It sure is");	//returns 1
        /// </pre>
        /// </summary>
        /// <param name="searchExpression">Search Expression</param>
        /// <param name="expressionSearched">Expression Searched</param>
        /// <returns>Line number</returns>
        public static int AtLine(string searchExpression, string expressionSearched)
        {
            var position = At(searchExpression, expressionSearched);
            return position > 0 && position < expressionSearched.Length ? Occurs(@"\r", SubStr(expressionSearched, 1, position - 1)) + 1 : 0;
        }

        /// <summary>
        /// Receives a string as a parameter and returns a bool indicating if the left most
        /// character in the string is a valid digit.
        /// <pre>
        /// Example:
        /// if(StringHelper.IsDigit("1Kamal")){...}	//returns true
        /// </pre>
        /// </summary>
        /// <param name="sourceString">Expression</param>
        /// <returns>True or False</returns>
        public static bool IsDigit(string sourceString) => sourceString.Length >= 1 && char.IsDigit(sourceString[0]);

        /// <summary>
        /// Takes a fully qualified file name, and returns just the path
        /// </summary>
        /// <param name="path">File name with path</param>
        /// <returns>Just the path as a string</returns>
        public static string JustPath(string path) => path.Substring(0, At("\\", path, Occurs("\\", path)) - 1);

        /// <summary>
        /// Makes sure the specified path ends with a back-slash
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>Path with BS</returns>
        public static string AddBS(string path)
        {
            if (!path.EndsWith("\\")) path += "\\";
            return path;
        }

        /// <summary>
        /// HTML-encodes the string and returns the encoded string.
        /// </summary>
        /// <param name="originalString">Original (unencoded) string</param>
        /// <returns>Encoded string</returns>
        public static string HtmlEncode(string originalString) => HttpUtility.HtmlEncode(originalString);

        /// <summary>
        /// HTML-decodes an HTML-encoded string.
        /// </summary>
        /// <param name="encodedString">Encoded string</param>
        /// <returns>Decoded string</returns>
        public static string HtmlDecode(string encodedString) => HttpUtility.HtmlDecode(encodedString);

        /// <summary>
        /// Returns true if the array contains the string we are looking for
        /// </summary>
        /// <param name="hostArray">The host array.</param>
        /// <param name="searchText">The search string.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>True or false</returns>
        /// <example>
        /// string[] testArray = new string[] { "One", "Two", "Three" };
        /// bool result1 = StringHelper.ArrayContainsString(testArray, "one", true); // returns true
        /// bool result2 = StringHelper.ArrayContainsString(testArray, "one"); // returns false
        /// bool result3 = StringHelper.ArrayContainsString(testArray, "One"); // returns true
        /// bool result4 = StringHelper.ArrayContainsString(testArray, "Four"); // returns false
        /// </example>
        public static bool ArrayContainsString(string[] hostArray, string searchText, bool ignoreCase = false)
        {
            var found = false;
            foreach (var item in hostArray)
                if (Compare(item, searchText, ignoreCase))
                {
                    found = true;
                    break;
                }

            return found;
        }

        /// <summary>
        /// Tries to parse a string value as an integer. If the parse fails,
        /// the provided default value will be inserted
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="failedDefault">The failed default.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "1";
        /// int valueInt = StringHelper.TryIntParse(value, -1);
        /// </example>
        public static int TryIntParse(string value, int failedDefault) => int.TryParse(value, out var parsedValue) ? parsedValue : failedDefault;

        /// <summary>
        /// Tries to parse a string value as an Guid. If the parse fails,
        /// the provided default value will be inserted
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="failedDefault">The failed default.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "xxx";
        /// Guid valueGuid = StringHelper.TryGuidParse(value, Guid.Empty);
        /// </example>
        public static Guid TryGuidParse(string value, Guid failedDefault) => Guid.TryParse(value, out var outGuid) ? outGuid : failedDefault;

        /// <summary>
        /// Tries to parse a string value as an Guid. If the parse fails,
        /// Guid.Empty will be returned
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <example>
        /// string value = "xxx";
        /// Guid valueGuid = StringHelper.TryGuidParse(value);
        /// </example>
        public static Guid TryGuidParse(string value) => TryGuidParse(value, Guid.Empty);
    }
}