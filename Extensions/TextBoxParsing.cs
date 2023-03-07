namespace VoiceAssistent.Extensions
{
    public static class TextBoxParsing
    {
        public static int ParseMethod_ToInt(string str)
        {
            int i;

            if (int.TryParse(str, out i) && i <= 99)
                return i;
            return 0;
        }
    }
}
