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

namespace IGE.Universe.QuestSystem {
	/// <summary>
	/// Description of QuestLogicGate.
	/// 
	/// Logic gates work exactly same way as quest objective except that they
	/// don't have conditions on output pins checked. As soon logic gate becomes
	/// active, all it's outputs become active too.
	/// </summary>
	public class QuestLogicGate : BaseQuestObject {
		public QuestLogicGate(ulong uid, string id) : base(uid, id) {
		}
	}
}
