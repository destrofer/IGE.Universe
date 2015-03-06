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
	/// Description of QuestObjective.
	/// </summary>
	public class QuestObjectState {
		public bool Locked = false;
		public ActivationState Active = ActivationState.Inactive;
		public CompletionState Completion = CompletionState.Incomplete; // used for quests, quest objectives and conditions
		public byte CompletionOutput = 0;
		
		public enum CompletionState : byte {
			Incomplete,
			Failed,
			Successful
		}
			
		public enum ActivationState : byte {
			Inactive,
			PartiallyActive, // Used only for input pins that have scripts, which determine whether the pin is supposed to actually be active or not 
			Active
		}
	}
}
