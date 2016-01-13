using System;
using System.Collections.Generic;
using System.Text;

namespace ocNet_Backup.core
{
	class Translation : ocNet.Lib.TranslationBase
	{
		static Translation translation;

		static public Translation Instance
		{
			get
			{
				if (translation == null)
					translation = new Translation();
				return translation;
			}
		}

		public Translation()
		{
		}

		protected override void AddTranslations()
		{
		}
	}
}
