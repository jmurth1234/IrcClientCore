namespace IrcClientCore
{
    public class User
    {
        public string Prefix
        {
            get
            {
                var prefix = "";

                if (FullUsername.Length > 2)
                {
                    prefix = FullUsername[0] + "" + FullUsername[1];
                }
                else if (FullUsername.Length > 1)
                {
                    prefix = FullUsername[0] + "";
                }

                return prefix;
            }
        }

        public string FullUsername { get; set; }

        public string Nick => FullUsername.Replace("~", "").Replace("&", "").Replace("@", "").Replace("%", "").Replace("+", "");

        public override string ToString()
        {
            return Nick;
        }

    }
}