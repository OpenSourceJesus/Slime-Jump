namespace SlimeJump
{
	public class Blaster : Weapon
	{
		public short maxXPosToAllowUse;
		public static Blaster instance;
		public static Blaster Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<Blaster>(true);
				return instance;
			}
			set
			{
				instance = value;
			}
		}
		public static bool Collected
		{
			get
			{
				return SaveAndLoadManager.GetBool("Collected blaster", false);
			}
			set
			{
				SaveAndLoadManager.SetBool ("Collected blaster", value);
			}
		}

		void Start ()
		{
			if (!Collected)
				gameObject.SetActive(false);
			else
				Player.instance.toggleShootBlasterImage.gameObject.SetActive(true);
		}

		public override void OnGain (Player player)
		{
			base.OnGain (player);
			player.SetMultiplyBlasterLaunchSpeedWithJump ();
		}

		public override string ToString ()
		{
			string output = base.ToString();
			BlasterBullet blasterBullet = (BlasterBullet) bulletPatternEntry.bulletPrefab;
			output += "Launch speed: " + blasterBullet.launchSpeed + '\n';
			return output;
		}
	}
}