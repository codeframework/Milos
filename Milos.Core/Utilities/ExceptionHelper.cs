using System;
using System.Text;

namespace Milos.Core.Utilities
{
    /// <summary>
    /// Various helper methods related to exceptions
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Analyzes exception information and returns HTML with details about the exception.
        /// </summary>
        /// <param name="exception">Exception object</param>
        /// <returns>Exception HTML</returns>
        public static string GetExceptionHtml(Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(@"<table><tr><td><font face=""Verdana"" size=""-1"">Exception Stack:</br>");
            var errorCount = -1;
            while (exception != null)
            {
                errorCount++;
                if (errorCount > 0) sb.Append("<br>");
                // + "icon"
                sb.Append("<b><span onclick=\"javascript:ShowError('error" + StringHelper.ToString(errorCount) + "','errorTable" + StringHelper.ToString(errorCount) + "');\" name=\"error" + StringHelper.ToString(errorCount) + "\" id=\"error" + StringHelper.ToString(errorCount) + "\">+</span>");
                // Error message
                sb.Append(@"&nbsp");
                sb.Append(exception.Message + "</b>");

                // Error detail
                sb.Append("<table width = \"100%\" id=\"errorTable" + StringHelper.ToString(errorCount) + "\" style=\"display:none\"><tr><td width=\"25\"> </td><td valign=\"top\" bgcolor=\"#ffffcc\"><font face=\"Tahoma\" size=\"-1\" color=\"maroon\"><b>");
                // Exception attributes
                sb.Append("Exception Attributes: <br><table>");
                // Header
                sb.Append("<tr><td> </td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">Exception Information: </font>");
                sb.Append("</td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">Exception Detail: </font>");
                sb.Append("</td></tr>");
                // Message
                sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">Message: ");
                sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                sb.Append(exception.Message);
                sb.Append("</td></tr>");
                // Exception type
                sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">ExceptionType: ");
                sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                sb.Append(StringHelper.ToString(exception.GetType()));
                sb.Append("</td></tr>");
                // Source
                sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">Source: ");
                sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                sb.Append(exception.Source);
                sb.Append("</td></tr>");
                if (exception.TargetSite != null)
                {
                    // Thrown by code in method
                    sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">ThrownByMethod: ");
                    sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                    sb.Append(exception.TargetSite.Name);
                    sb.Append("</td></tr>");
                    // Thrown by code in method
                    sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">ThrownByClass: ");
                    sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                    if (exception.TargetSite.DeclaringType != null) sb.Append(exception.TargetSite.DeclaringType.Name);
                    sb.Append("</td></tr>");
                    sb.Append("</table>");
                }

                // Stack Trace
                sb.Append("StackTrace: <br><table>");
                // Header
                sb.Append("<tr><td> </td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">LineNumber: </font>");
                sb.Append("</td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">Method: </font>");
                sb.Append("</td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">SourceFile: </font>");
                sb.Append("</td></tr>");
                // Actual stack trace
                var stackLines = exception.StackTrace.Split('\r');
                foreach (var stackLine in stackLines)
                    if (stackLine.IndexOf(" in ", StringComparison.Ordinal) > -1)
                    {
                        // We have detailed info
                        // We only have generic info
                        var detail = stackLine.Trim().Trim();
                        detail = detail.Replace("at ", string.Empty);
                        var at = detail.IndexOf(" in ", StringComparison.Ordinal);
                        var file = detail.Substring(at + 4);
                        detail = detail.Substring(0, at);
                        at = file.IndexOf(":line", StringComparison.Ordinal);
                        var sLine = file.Substring(at + 6);
                        file = file.Substring(0, at);
                        sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">");
                        sb.Append(sLine);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                        sb.Append(detail);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                        sb.Append(file);
                        sb.Append("</td></tr>");
                    }
                    else
                    {
                        // We only have generic info
                        var detail = stackLine.Trim().Trim();
                        detail = detail.Replace("at ", string.Empty);
                        sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\" color=\"darkgray\">NotApplicable");
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\" color=\"darkgray\">");
                        sb.Append(detail);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\" color=\"darkgray\">");
                        sb.Append("</td></tr>");
                    }

                sb.Append("</table>");

                // Closing the table
                sb.Append("</td></tr></table>");

                // Next exception
                exception = exception.InnerException;
            }

            sb.Append(@"</font></td></tr></table>");
            // Script needed to expand and collapse
            sb.AppendLine("\r\n<script language=\"JavaScript\">");
            sb.AppendLine("function ShowError(id,idTable) {");
            sb.AppendLine("   var obj = document.getElementById(idTable);");
            sb.AppendLine("   var obj2 = document.getElementById(id);");
            sb.AppendLine("   if (obj.style.display == \"none\") {");
            sb.AppendLine("       obj2.innerHTML = \"-\";");
            sb.AppendLine("       obj.style.display = \"\";\r\n   }");
            sb.AppendLine("   else {");
            sb.AppendLine("       obj2.innerHTML = \"+\";");
            sb.AppendLine("       obj.style.display = \"none\";\r\n   }");
            sb.AppendLine("}");
            sb.AppendLine("</script>");
            return sb.ToString();
        }

        /// <summary>
        /// Analyzes exception information and returns it as a plain text string
        /// </summary>
        /// <param name="exception">Exception object</param>
        /// <returns>string</returns>
        public static string GetExceptionText(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ExceptionStack:");
            sb.AppendLine();
            var errorCount = -1;
            while (exception != null)
            {
                errorCount++;
                if (errorCount > 0) sb.AppendLine();
                sb.Append(exception.Message);

                // Error detail
                sb.AppendLine("  Exception Attributes:");
                sb.AppendLine("    Message: " + exception.Message);
                sb.AppendLine("    Exception Type: " + exception.GetType());
                sb.AppendLine("    Source: " + exception.Source);
                if (exception.TargetSite != null)
                {
                    sb.AppendLine("    Thrown by Method: " + exception.TargetSite.Name);
                    if (exception.TargetSite.DeclaringType != null)
                        sb.AppendLine("    Thrown by Class/Type: " + exception.TargetSite.DeclaringType.Name);
                }

                sb.AppendLine("  StackTrace:");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    var stackLines = exception.StackTrace.Split('\r');
                    foreach (var stackLine in stackLines)
                        if (!string.IsNullOrEmpty(stackLine))
                            if (stackLine.IndexOf(" in ", StringComparison.Ordinal) > -1)
                            {
                                // We have detailed info
                                // We only have generic info
                                var detail = stackLine.Trim().Trim();
                                detail = detail.Replace("at ", string.Empty);
                                var at = detail.IndexOf(" in ", StringComparison.Ordinal);
                                var file = detail.Substring(at + 4);
                                detail = detail.Substring(0, at);
                                at = file.IndexOf(":line", StringComparison.Ordinal);
                                var lineNumber = file.Substring(at + 6);
                                file = file.Substring(0, at);
                                sb.Append("    Line Number: " + lineNumber + " -- ");
                                sb.Append("Method: " + detail + " -- ");
                                sb.Append("SourceFile: " + file + "\r\n");
                            }
                            else
                            {
                                // We only have generic info
                                var detail = stackLine.Trim().Trim();
                                detail = detail.Replace("at ", string.Empty);
                                sb.Append("    Method: " + detail);
                            }
                }

                // Next exception
                exception = exception.InnerException;
            }

            return sb.ToString();
        }
    }
}