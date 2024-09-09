// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Dialogue
{
    using System;
    using System.Collections.Generic;

    public static class DialogueTextProcessor
    {
        /// <summary>
        /// Break long dialogue into pieces
        /// Basically separate a dialogue into two pieces if there are more
        /// than three new line chars found in the dialogue text.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>One or two sub dialogues</returns>
        public static IEnumerable<string> GetSubDialoguesAsync(string text)
        {
            if (text.Contains('\n'))
            {
                int indexOfSecondNewLineChar = text.IndexOf('\n', text.IndexOf('\n') + 1);
                if (indexOfSecondNewLineChar != -1)
                {
                    int indexOfThirdNewLineChar = text.IndexOf('\n', indexOfSecondNewLineChar + 1);
                    if (indexOfThirdNewLineChar != -1 && indexOfThirdNewLineChar != text.Length)
                    {
                        string firstPart = text[..indexOfThirdNewLineChar];
                        string secondPart = text.Substring(indexOfThirdNewLineChar, text.Length - indexOfThirdNewLineChar);
                        yield return firstPart;
                        yield return text[..text.IndexOf('\n')] + secondPart;
                        yield break;
                    }
                }
            }

            yield return text;
        }

        /// <summary>
        /// Returns the display text with information text color used by TextMeshProUGUI.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <param name="informationTextColorHex">The hex code for the information text color.</param>
        /// <returns>The formatted text with information text color.</returns>
        public static string GetDisplayText(string text, string informationTextColorHex)
        {
            string formattedText = text.Replace("\\n", "\n");

            return ReplaceStringInTextWithPatternForEachChar(formattedText,
                "\\i", "\\r",
                $"<color={informationTextColorHex}>", "</color>");
        }

        /// <summary>
        /// Returns the display text for a given selection text. Removes any numbering or
        /// punctuation at the beginning or end of the selection text.
        /// </summary>
        /// <param name="selectionText">The selection text to process.</param>
        /// <returns>The display text for the selection string.</returns>
        public static string GetSelectionDisplayText(string selectionText)
        {
            if (selectionText.EndsWith("；") || selectionText.EndsWith("。")) selectionText = selectionText[..^1];

            if (selectionText.Contains('.'))
            {
                string numberStr = selectionText[..selectionText.IndexOf('.')];
                if (int.TryParse(numberStr, out _))
                {
                    return selectionText[(selectionText.IndexOf('.') + 1)..];
                }
            }

            if (selectionText.Contains('、'))
            {
                string numberStr = selectionText[..selectionText.IndexOf('、')];
                if (int.TryParse(numberStr, out _))
                {
                    return selectionText[(selectionText.IndexOf('、') + 1)..];
                }
            }

            // I don't think there will be more than 20 options, so let's start with 20
            for (int i = 20; i >= 0; i--)
            {
                string intStr = i.ToString();
                if (selectionText.StartsWith(intStr) && !string.Equals(selectionText, intStr))
                {
                    return selectionText[intStr.Length..];
                }
            }

            return selectionText;
        }

        /// <summary>
        /// Replaces each character in a substring of the input string that is enclosed by a start pattern and an end pattern with a new pattern.
        /// </summary>
        /// <param name="text">The input string to process.</param>
        /// <param name="startPattern">The start pattern that marks the beginning of the substring to process.</param>
        /// <param name="endPattern">The end pattern that marks the end of the substring to process.</param>
        /// <param name="charStartPattern">The pattern to insert before each character in the substring.</param>
        /// <param name="charEndPattern">The pattern to insert after each character in the substring.</param>
        /// <returns>The processed string.</returns>
        private static string ReplaceStringInTextWithPatternForEachChar(string text,
            string startPattern,
            string endPattern,
            string charStartPattern,
            string charEndPattern)
        {
            string newStr = string.Empty;

            int currentIndex = 0;
            int startOfInformation = text.IndexOf(startPattern, StringComparison.Ordinal);
            while (startOfInformation != -1)
            {
                int endOfInformation = text.IndexOf(endPattern, startOfInformation, StringComparison.Ordinal);

                newStr += text.Substring(currentIndex, startOfInformation - currentIndex);

                foreach (char ch in text.Substring(
                             startOfInformation + startPattern.Length,
                             endOfInformation - startOfInformation - startPattern.Length))
                {
                    newStr += $"{charStartPattern}{ch}{charEndPattern}";
                }

                currentIndex = endOfInformation + endPattern.Length;
                startOfInformation = text.IndexOf(
                    startPattern, currentIndex, StringComparison.Ordinal);
            }

            newStr += text.Substring(currentIndex, text.Length - currentIndex);

            return newStr;
        }
    }
}