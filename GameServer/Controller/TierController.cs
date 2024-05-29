using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class TierController
{
    private static TierController instance;
    public static TierController Instance
    {
        get
        {
            if (instance == null)
            {
                if (instance == null)
                {
                    instance = new TierController();
                }
            }
            return instance;

        }
    }

    RatingRange ratingRange = new RatingRange();
    public void Init()
    {
        Console.WriteLine($"{this.ToString()} init Complete");
    }
}
