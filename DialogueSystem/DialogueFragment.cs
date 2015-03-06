/*
 * Author: Viacheslav Soroka
 * 
 * This file is part of IGE <https://github.com/destrofer/IGE>.
 * 
 * IGE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * IGE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with IGE.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace IGE.Universe.DialogueSystem {
	/// <summary>
	/// Description of Dialogues.
	/// </summary>
	public class DialogueFragment : BaseDialogueObject {
		protected ulong m_ActorId;
		protected string m_MenuText;
		protected string m_Text;
		protected string m_Extra;
		
		public ulong ActorId { get { return m_ActorId; } set { m_ActorId = value; } }
		public string MenuText { get { return m_MenuText; } set { m_MenuText = value; } }
		public string Text { get { return m_Text; } set { m_Text = value; } }
		public string Extra { get { return m_Extra; } set { m_Extra = value; } }
	
		public DialogueFragment(ulong id, string key) : base(id, key) {
		}
	}
}
