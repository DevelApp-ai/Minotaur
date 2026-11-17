/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Text;

namespace Minotaur.Parser.LiveEditing
{
    /// <summary>
    /// A single replacement edit at a text offset, in the byte-offset
    /// semantics of <c>RealTimeParserSession.ApplyEdit</c>: delete
    /// <see cref="DeleteLength"/> bytes starting at <see cref="Offset"/>
    /// and insert <see cref="InsertText"/> at <see cref="Offset"/>.
    /// </summary>
    /// <param name="Offset">Zero-based offset of the edit in the current text.</param>
    /// <param name="DeleteLength">Number of bytes to delete starting at the offset.</param>
    /// <param name="InsertText">The text inserted at the offset (may be empty).</param>
    public readonly record struct TextEdit(int Offset, int DeleteLength, string InsertText);

    /// <summary>
    /// Computes the single <see cref="TextEdit"/> that transforms one text
    /// into another, by trimming the common prefix and suffix. This is the
    /// standard way to turn an editor's old-value/new-value pair into the
    /// incremental edit expected by the engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Offsets follow the engine's documented UTF-8 byte-offset semantics.
    /// The shipped <c>RealTimeParserSession</c> splices the source with the
    /// same offset value, so byte offsets and character offsets must agree;
    /// this class therefore only accepts ASCII text and throws
    /// <see cref="NotSupportedException"/> otherwise. Non-ASCII support
    /// requires an engine-side change and is tracked upstream.
    /// </para>
    /// <para>
    /// The result is a single replace-region edit (the minimal middle
    /// between the common prefix and the common suffix), which is exactly
    /// the shape produced by typing, deletion, completion and paste
    /// operations in a text editor.
    /// </para>
    /// </remarks>
    public static class TextEditCalculator
    {
        /// <summary>
        /// Compute the edit that transforms <paramref name="oldText"/> into
        /// <paramref name="newText"/>, or <see langword="null"/> when the
        /// two texts are identical (no edit).
        /// </summary>
        /// <param name="oldText">The previous text.</param>
        /// <param name="newText">The updated text.</param>
        /// <returns>The minimal replacement edit, or <see langword="null"/> when nothing changed.</returns>
        /// <exception cref="ArgumentNullException">A text is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Either text contains non-ASCII characters.</exception>
        public static TextEdit? Compute(string oldText, string newText)
        {
            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            if (ReferenceEquals(oldText, newText) || oldText == newText)
            {
                return null;
            }

            RequireAscii(oldText, nameof(oldText));
            RequireAscii(newText, nameof(newText));

            // Longest common prefix.
            var prefix = 0;
            var maxPrefix = Math.Min(oldText.Length, newText.Length);
            while (prefix < maxPrefix && oldText[prefix] == newText[prefix])
            {
                prefix++;
            }

            // Longest common suffix that does not overlap the prefix.
            var suffix = 0;
            var maxSuffix = Math.Min(oldText.Length - prefix, newText.Length - prefix);
            while (suffix < maxSuffix && oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
            {
                suffix++;
            }

            var deleteLength = oldText.Length - prefix - suffix;
            var insertLength = newText.Length - prefix - suffix;

            return new TextEdit(
                Offset: prefix,
                DeleteLength: deleteLength,
                InsertText: insertLength == 0 ? string.Empty : newText.Substring(prefix, insertLength));
        }

        /// <summary>
        /// Verify that a text is pure ASCII (byte and character offsets
        /// then coincide, matching the engine's offset semantics).
        /// </summary>
        /// <param name="text">The text to verify.</param>
        /// <param name="paramName">The parameter name used in the exception.</param>
        /// <exception cref="NotSupportedException">The text contains a character above 0x7F.</exception>
        public static void RequireAscii(string text, string paramName)
        {
            if (text is null)
            {
                throw new ArgumentNullException(paramName);
            }

            foreach (var c in text)
            {
                if (c > (char)0x7F)
                {
                    throw new NotSupportedException(
                        "Non-ASCII text is not supported by the live editor session: the shipped " +
                        "RealTimeParserSession documents UTF-8 byte offsets but splices the source with " +
                        "the same value, which is only equivalent for ASCII content. Remove the non-ASCII " +
                        "character (0x" + ((int)c).ToString("X4") + ") or use ASCII content.");
                }
            }
        }

        /// <summary>
        /// Check whether a text is pure ASCII without throwing.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns><see langword="true"/> when every character is at or below 0x7F.</returns>
        public static bool IsAscii(string text)
        {
            if (text is null)
            {
                return true;
            }

            foreach (var c in text)
            {
                if (c > (char)0x7F)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
