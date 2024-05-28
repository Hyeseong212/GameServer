using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class MatchController
{
    private static MatchController instance;
    public static MatchController Instance
    {
        get
        {
            if (instance == null)
            {
                if (instance == null)
                {
                    instance = new MatchController();
                }
            }
            return instance;

        }
    }
    public void Init()
    {
        Console.WriteLine($"{this.ToString()} init Complete");
    }
}
