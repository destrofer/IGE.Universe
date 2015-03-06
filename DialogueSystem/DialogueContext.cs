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
using System.Collections.Generic;

using IGE.Scripting;

namespace IGE.Universe.DialogueSystem {
	/// <summary>
	/// Description of DialogueContext.
	/// </summary>
	public class DialogueContext : DialogueAndQuestSystemScriptContext {
		public BaseGameObject Actor = null;
		public DialogueFragment[] AvailableChoices = null;
		
		public event ActorTalkEventHandler OnActorTalk;
		
		public DialogueContext() {
		}
		
		/// <summary>
		/// Looks for dialogue fragments and builds list of next available menu choices 
		/// </summary>
		/// <param name="fromPin"></param>
		/// <returns>TRUE if there was a connection to the end of dialogue</returns>
		public bool BuildChoices(DialoguePin fromPin) {
			List<DialogueFragment> fragments = new List<DialogueFragment>();
			bool endDialogue = BuildChoices(fromPin, fragments);
			if( endDialogue || fragments.Count == 0 ) {
				endDialogue = true;
				AvailableChoices = null;
			}
			else
				AvailableChoices = fragments.ToArray();
			return endDialogue;
		}

		public bool BuildChoices(BaseDialogueObject[] fromObjects) {
			List<DialogueFragment> fragments = new List<DialogueFragment>();
			bool endDialogue = false;
			
			foreach( BaseDialogueObject fromObject in fromObjects ) {
				if( fromObject is DialogueFragment ) {
					foreach( GraphPin pin in fromObject.Outputs ) {
						if( BuildChoices(pin, fragments) ) {
							endDialogue = true;
							break;
						}
					}
				}
				else
					endDialogue = BuildChoices(fromObject, fragments);
				if( endDialogue )
					break;
			}
			
			if( endDialogue || fragments.Count == 0 ) {
				endDialogue = true;
				AvailableChoices = null;
			}
			else
				AvailableChoices = fragments.ToArray();
			
			return endDialogue;
		}
		
		public bool BuildChoices(List<BaseDialogueObject> fromObjects) {
			return BuildChoices(fromObjects.ToArray());
		}

	
		public bool BuildChoices(BaseDialogueObject fromObject) {
			return BuildChoices(new BaseDialogueObject[] { fromObject });
		}

		protected bool BuildChoices(BaseDialogueObject fromObject, List<DialogueFragment> fragments) {
			IList<GraphPin> pins;
			if( fromObject is Dialogue )
				pins = fromObject.Inputs;
			else if( fromObject is DialogueCondition ) {
				DialogueCondition condition = (DialogueCondition)fromObject;
				if( condition.Script == null ) {
					GameDebugger.Log(LogLevel.Warning, "Condition '0x{0:X16}' has no script", condition.Id);
					return true;
				}
				dynamic result = condition.Script.Run(this);
				if( !(result is Boolean) ) {
					GameDebugger.Log(LogLevel.Warning, "Condition '0x{0:X16}' script exits with a non boolean result ({1})", condition.Id, result.GetType().Name);
					return true; // end the dialogue due to error
				}
				pins = new List<GraphPin>();
				pins.Add(condition.Outputs[result ? 0 : 1]);
			}
			else {
				if( fromObject is DialogueFragment ) {
					DialogueFragment fragment = (DialogueFragment)fromObject;
					if( !string.IsNullOrWhiteSpace(fragment.MenuText) ) {
						fragments.Add(fragment);
						return false;
					}

					if( OnActorTalk != null )
						OnActorTalk.Invoke(new ActorTalkEventArgs(fragment));
				}
				pins = fromObject.Outputs;
			}
			
			if( fromObject is DialogueInstruction ) {
				DialogueInstruction instruction = (DialogueInstruction)fromObject;
				if( instruction.Script != null )
					instruction.Script.Run(this);
			}
			
			if( fromObject is DialogueJump ) {
				DialogueJump jump = (DialogueJump) fromObject;
				if( jump.TargetPin != null ) {
					if( BuildChoices(jump.TargetPin, fragments) )
						return true;
				}
				else if( jump.Target != null ) {
					if( BuildChoices(jump.Target, fragments) )
						return true;
				}
			}
			
			foreach( GraphPin gpin in pins ) {
				if( BuildChoices(gpin, fragments) )
					return true;
			}
			return false;
		}
		
		protected bool BuildChoices(GraphPin inputPin, List<DialogueFragment> fragments) {
			DialoguePin pin = inputPin as DialoguePin;
			if( pin == null )
				return false;
			
			BaseDialogueObject obj = pin.Node as BaseDialogueObject;
			if( obj == null )
				return false;
			
			if( pin.Type == GraphPin.PinType.Output && obj is Dialogue )
				return true; // connection to a dialogue output is reached so it's the end of the dialogue
			
			bool processObject = true;
			if( pin.Script != null ) {
				dynamic result = pin.Script.Run(this);
				if( pin.Type == GraphPin.PinType.Input ) {
					if( !(result is Boolean) ) {
						GameDebugger.Log(LogLevel.Warning, "One of input pins script on dialogue object '0x{0:X16}' exits with a non boolean result ({1})", obj.Id, result.GetType().Name);
						return true; // end the dialogue due to error
					}
					if( !result ) // input pin is a condition. if it returns false, then object is not processed
						processObject = false;
				}
			}

			if( processObject && !(obj is Dialogue) && pin.Type == GraphPin.PinType.Input ) {
				if( BuildChoices(obj, fragments) )
					return true;
			}
			
			foreach( GraphConnection conn in pin.Connections ) {
				if( conn.Target == pin )
					continue; // ignore incoming connections
				if( BuildChoices(conn.Target, fragments) ) // recursion through connections
					return true;
			}
			
			return false;
		}
	}

	public delegate void ActorTalkEventHandler(ActorTalkEventArgs args);
	
	public class ActorTalkEventArgs : EventArgs {
		public DialogueFragment Fragment = null;
		public ActorTalkEventArgs(DialogueFragment fragment) {
			Fragment = fragment;
		}
	}
}
