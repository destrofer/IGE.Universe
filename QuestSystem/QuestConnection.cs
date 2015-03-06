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
	public class QuestConnection : GraphConnection, IQuestSystemObject {
		protected ulong m_Id;
		protected string m_Key;
	
		public ulong Id { get { return m_Id; } }
		public string Key { get { return m_Key; } }
		
		public QuestConnection(ulong id, string key) {
			m_Id = id;
			m_Key = key;
		}
	}
}