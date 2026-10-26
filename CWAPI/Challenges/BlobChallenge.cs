using CWAPI.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CWAPI.Challenges;

public class BlobChallenge : ModdedChallenge
{
    public BlobChallenge()
    {
        this.ChallengeName = "Blob";
        this.Description = "Blob";
    }

    public override Type ChallengeUnlock => typeof(BlobPin);
}
