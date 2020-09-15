using UnityEngine;

namespace SixModLoader.Api.Extensions
{
    public static class FormatExtensions
    {
        /// <summary>
        /// Wraps <paramref name="text"/> with <paramref name="tag"/> with optional <paramref name="value"/>
        /// </summary>
        /// <param name="text">Text to be wrapped in tag</param>
        /// <param name="tag">Tag of wrapped text</param>
        /// <param name="value">Value of tag</param>
        /// <returns>Wrapped text</returns>
        public static string Tag(this string text, string tag, string value = null)
        {
            return $"<{tag}{(value == null ? string.Empty : $"={value}")}>{text}</{tag}>";
        }

        /// <summary>
        /// Changes size of <paramref name="text"/>
        /// </summary>
        /// <param name="text">Text to change size</param>
        /// <param name="value">Size of text</param>
        /// <returns>Text with changed size</returns>
        public static string Size(this string text, int value)
        {
            return text.Tag("size", value.ToString());
        }

        /// <summary>
        /// Changes color of <paramref name="text"/>
        /// </summary>
        /// <param name="text">Text to change color</param>
        /// <param name="value">Color of text (<see cref="UnityEngine.Color"/>)</param>
        /// <returns>Text with changed color</returns>
        public static string Color(this string text, Color value)
        {
            return text.Color(value.ToHex());
        }

        /// <summary>
        /// Changes color of <paramref name="text"/>
        /// </summary>
        /// <param name="text">Text to change color</param>
        /// <param name="value">Color of text (hex)</param>
        /// <returns>Text with changed color</returns>
        public static string Color(this string text, string value)
        {
            return text.Tag("color", value);
        }

        /// <summary>
        /// Changes color of <paramref name="text"/>
        /// </summary>
        /// <param name="text">Text to change color</param>
        /// <param name="player">Color of text (player class color)</param>
        /// <returns>Text with changed color</returns>
        public static string Color(this string text, ReferenceHub player)
        {
            return text.Color(player.characterClassManager.CurRole);
        }

        public static string Color(this string text, RoleType roleType)
        {
            return text.Color(CharacterClassManager._staticClasses.SafeGet(roleType));
        }

        public static string Color(this string text, Role role)
        {
            return text.Color(role.classColor);
        }

        /// <summary>
        /// Changes vertical offset of <paramref name="text"/>
        /// </summary>
        /// <param name="text">Text to change vertical offset</param>
        /// <param name="value">Vertical offset</param>
        /// <returns>Text with changed vertical offset</returns>
        public static string VerticalOffset(this string text, string value)
        {
            return text.Tag("voffset", value);
        }

        /// <summary>
        /// Changes vertical offset of <paramref name="text"/>
        /// </summary>
        /// <param name="text">Text to change vertical offset</param>
        /// <param name="value">Vertical offset (em)</param>
        /// <returns>Text with changed vertical offset</returns>
        public static string VerticalOffset(this string text, int value)
        {
            return text.VerticalOffset(value + "em");
        }
    }
}