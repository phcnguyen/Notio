using Nalix.Network.Web.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nalix.Network.Web.Routing
{
    /// <summary>
    /// Provides utility methods to work with routes.
    /// </summary>
    /// <seealso cref="WebApiModule"/>
    /// <seealso cref="WebApiController"/>
    /// <seealso cref="RouteAttribute"/>
    public static class Route
    {
        // Characters in ValidParameterNameChars MUST be in ascending ordinal order!
        private static readonly char[] ValidParameterNameChars =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// <para>Determines whether a string is a valid route parameter name.</para>
        /// <para>To be considered a valid route parameter name, the specified string:</para>
        /// <list type="bullet">
        /// <item><description>must not be <see langword="null"/>;</description></item>
        /// <item><description>must not be the empty string;</description></item>
        /// <item><description>must consist entirely of decimal digits, upper- or lower-case
        /// letters of the English alphabet, or underscore (<c>'_'</c>) characters;</description></item>
        /// <item><description>must not start with a decimal digit.</description></item>
        /// </list>
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a valid route parameter;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsValidParameterName(string value)
        {
            return !string.IsNullOrEmpty(value)
                    && value[0] > '9'
                    && !value.Any(c => c < '0' || c > 'z' || Array.BinarySearch(ValidParameterNameChars, c) < 0);
        }

        /// <summary>
        /// <para>Determines whether a string is a valid route.</para>
        /// <para>To be considered a valid route, the specified string:</para>
        /// <list type="bullet">
        /// <item><description>must not be <see langword="null"/>;</description></item>
        /// <item><description>must not be the empty string;</description></item>
        /// <item><description>must start with a slash (<c>'/'</c>) character;</description></item>
        /// <item><description>if a base route, must end with a slash (<c>'/'</c>) character;</description></item>
        /// <item><description>if not a base route, must not end with a slash (<c>'/'</c>) character,
        /// unless it is the only character in the string;</description></item>
        /// <item><description>must not contain consecutive runs of two or more slash (<c>'/'</c>) characters;</description></item>
        /// <item><description>may contain one or more parameter specifications.</description></item>
        /// </list>
        /// <para>Each parameter specification must be enclosed in curly brackets (<c>'{'</c>
        /// and <c>'}'</c>. No whitespace is allowed inside a parameter specification.</para>
        /// <para>Two parameter specifications must be separated by literal text.</para>
        /// <para>A parameter specification consists of a valid parameter name, optionally
        /// followed by a <c>'?'</c> character to signify that it will also match an empty string.</para>
        /// <para>If <c>'?'</c> is not present, a parameter by default will NOT match an empty string.</para>
        /// <para>See <see cref="IsValidParameterName"/> for the definition of a valid parameter name.</para>
        /// <para>To include a literal open curly bracket in the route, it must be doubled (<c>"{{"</c>).</para>
        /// <para>A literal closed curly bracket (<c>'}'</c>) may be included in the route as-is.</para>
        /// <para>A segment of a base route cannot consist only of an optional parameter.</para>
        /// </summary>
        /// <param name="route">The route to check.</param>
        /// <param name="isBaseRoute"><see langword="true"/> if checking for a base route;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="route"/> is a valid route;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(string route, bool isBaseRoute)
        {
            return ValidateInternal(nameof(route), route, isBaseRoute) == null;
        }

        // Check the validity of a route by parsing it without storing the results.
        // Returns: ArgumentNullException, ArgumentException, null if OK
        internal static Exception? ValidateInternal(string argumentName, string value, bool isBaseRoute)
        {
            return ParseInternal(value, isBaseRoute, null) switch
            {
                ArgumentNullException _ => new ArgumentNullException(argumentName),
                FormatException formatException => new ArgumentException(formatException.Message, argumentName),
                Exception exception => exception,
                _ => null
            };
        }

        // Validate and parse a route, constructing a Regex pattern.
        // setResult will be called at the end with the isBaseRoute flag, parameter names and the constructed pattern.
        // Returns: ArgumentNullException, FormatException, null if OK
        internal static Exception? ParseInternal(string route, bool isBaseRoute, Action<bool, IEnumerable<string>, string>? setResult)
        {
            if (route == null)
            {
                return new ArgumentNullException(nameof(route));
            }

            if (route.Length == 0)
            {
                return new FormatException("Route is empty.");
            }

            if (route[0] != '/')
            {
                return new FormatException("Route does not start with a slash.");
            }

            /*
             * Regex options set at start of pattern:
             * IgnoreCase              : no
             * Multiline               : no
             * Singleline              : yes
             * ExplicitCapture         : yes
             * IgnorePatternWhitespace : no
             * See https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options
             * See https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#group_options
             */
            const string InitialRegexOptions = "(?sn-imx)";

            // If setResult is null we don't need the StringBuilder.
            StringBuilder? sb = setResult == null ? null : new StringBuilder("^");

            List<string> parameterNames = [];
            if (route.Length == 1)
            {
                // If the route consists of a single slash, only a single slash will match.
                _ = (sb?.Append(isBaseRoute ? "/" : "/$"));
            }
            else
            {
                // First of all divide the route in segments.
                // Segments are separated by slashes.
                // The route is not necessarily normalized, so there could be runs of consecutive slashes.
                int segmentCount = 0;
                int optionalSegmentCount = 0;
                foreach (string segment in GetSegments(route))
                {
                    segmentCount++;

                    // Parse the segment, looking alternately for a '{', that opens a parameter specification,
                    // then for a '}', that closes it.
                    // Characters outside parameter specifications are Regex-escaped and added to the pattern.
                    // A parameter specification consists of a parameter name, optionally followed by '?'
                    // to indicate that an empty parameter will match.
                    // The default is to NOT match empty parameters, consistently with ASP.NET and Notio version 2.
                    // More syntax rules:
                    // - There cannot be two parameters without literal text in between.
                    // - If a segment consists ONLY of an OPTIONAL parameter, then the slash preceding it is optional too.
                    bool inParameterSpec = false;
                    bool afterParameter = false;
                    for (int position = 0; ;)
                    {
                        if (inParameterSpec)
                        {
                            // Look for end of spec, bail out if not found.
                            int closePosition = segment.IndexOf('}', position);
                            if (closePosition < 0)
                            {
                                return new FormatException("Route syntax error: unclosed parameter specification.");
                            }

                            // Parameter spec cannot be empty.
                            if (closePosition == position)
                            {
                                return new FormatException("Route syntax error: empty parameter specification.");
                            }

                            // Check the last character:
                            // {name} means empty parameter does not match
                            // {name?} means empty parameter matches
                            // If '?'is found, the parameter name ends before it
                            int nameEndPosition = closePosition;
                            bool allowEmpty = false;
                            if (segment[closePosition - 1] == '?')
                            {
                                allowEmpty = true;
                                nameEndPosition--;
                            }

                            // Bail out if only '?' is found inside the spec.
                            if (nameEndPosition == position)
                            {
                                return new FormatException("Route syntax error: missing parameter name.");
                            }

                            // Extract the parameter name.
                            string parameterName = segment[position..nameEndPosition];

                            // Ensure that the parameter name contains only valid characters.
                            if (!IsValidParameterName(parameterName))
                            {
                                return new FormatException("Route syntax error: parameter name contains one or more invalid characters.");
                            }

                            // Ensure that the parameter name is not a duplicate.
                            if (parameterNames.Contains(parameterName))
                            {
                                return new FormatException("Route syntax error: duplicate parameter name.");
                            }

                            // The spec is valid, so add the parameter to the list.
                            parameterNames.Add(parameterName);

                            // Append a capturing group with the same name to the pattern.
                            // Parameters must be made of non-slash characters ("[^/]")
                            // and must match non-greedily ("*?" if optional, "+?" if non optional).
                            // Position will be 1 at the start, not 0, because we've skipped the opening '{'.
                            if (allowEmpty && position == 1 && closePosition == segment.Length - 1)
                            {
                                if (isBaseRoute)
                                {
                                    return new FormatException("No segment of a base route can be optional.");
                                }

                                // If the segment consists only of an optional parameter,
                                // then the slash preceding the segment is optional as well.
                                // In this case the parameter must match only is not empty,
                                // because it's (slash + parameter) that is optional.
                                _ = (sb?.Append("(/(?<").Append(parameterName).Append(">[^/]+?))?"));
                                optionalSegmentCount++;
                            }
                            else
                            {
                                // If at the start of a segment, don't forget the slash!
                                // Position will be 1 at the start, not 0, because we've skipped the opening '{'.
                                if (position == 1)
                                {
                                    _ = (sb?.Append('/'));
                                }

                                _ = (sb?.Append("(?<").Append(parameterName).Append(">[^/]").Append(allowEmpty ? '*' : '+').Append("?)"));
                            }

                            // Go on with parsing.
                            position = closePosition + 1;
                            inParameterSpec = false;
                            afterParameter = true;
                        }
                        else
                        {
                            // Look for start of parameter spec.
                            int openPosition = segment.IndexOf('{', position);
                            if (openPosition < 0)
                            {
                                // If at the start of a segment, don't forget the slash.
                                if (position == 0)
                                {
                                    _ = (sb?.Append('/'));
                                }

                                // No more parameter specs: escape the remainder of the string
                                // and add it to the pattern.
                                _ = (sb?.Append(Regex.Escape(segment[position..])));
                                break;
                            }

                            int nextPosition = openPosition + 1;
                            if (nextPosition < segment.Length && segment[nextPosition] == '{')
                            {
                                // If another identical char follows, treat the two as a single literal char.
                                // If at the start of a segment, don't forget the slash!
                                if (position == 0)
                                {
                                    _ = (sb?.Append('/'));
                                }

                                _ = (sb?.Append(@"\\{"));
                            }
                            else if (afterParameter && openPosition == position)
                            {
                                // If a parameter immediately follows another parameter,
                                // with no literal text in between, it's a syntax error.
                                return new FormatException("Route syntax error: parameters must be separated by literal text.");
                            }
                            else
                            {
                                // If at the start of a segment, don't forget the slash,
                                // but only if there actually is some literal text.
                                // Otherwise let the parameter spec parsing code deal with the slash,
                                // because we don't know whether this is an optional segment yet.
                                if (position == 0 && openPosition > 0)
                                {
                                    _ = (sb?.Append('/'));
                                }

                                // Escape the part of the pattern outside the parameter spec
                                // and add it to the pattern.
                                _ = (sb?.Append(Regex.Escape(segment[position..openPosition])));
                                inParameterSpec = true;
                            }

                            // Go on parsing.
                            position = nextPosition;
                            afterParameter = false;
                        }
                    }
                }

                // Close the pattern
                _ = (sb?.Append(isBaseRoute ? "(/|$)" : "$"));

                // If all segments are optional segments, "/" must match too.
                if (optionalSegmentCount == segmentCount)
                {
                    _ = (sb?.Insert(0, "(/$)|(").Append(')'));
                }
            }

            // Pass the results to the callback if needed.
            setResult?.Invoke(isBaseRoute, parameterNames, InitialRegexOptions + sb);

            // Everything's fine, thus no exception.
            return null;
        }

        // Enumerate the segments of a route, ignoring consecutive slashes.
        private static IEnumerable<string> GetSegments(string route)
        {
            int length = route.Length;
            int position = 0;
            for (; ; )
            {
                while (route[position] == '/')
                {
                    position++;
                    if (position >= length)
                    {
                        break;
                    }
                }

                if (position >= length)
                {
                    break;
                }

                int slashPosition = route.IndexOf('/', position);
                if (slashPosition < 0)
                {
                    yield return route[position..];
                    break;
                }

                yield return route[position..slashPosition];
                position = slashPosition;
            }
        }
    }
}