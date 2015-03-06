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

namespace IGE.Universe.QuestSystem {
	/// <summary>
	/// Description of QuestLog.
	/// </summary>
	public class QuestLog : DialogueAndQuestSystemScriptContext {
		public event QuestActivatedEventHandler OnQuestActivated;
		public event QuestObjectiveActivatedEventHandler OnQuestObjectiveActivated;
		public event QuestDeactivatedEventHandler OnQuestDeactivated;
		public event QuestObjectiveDeactivatedEventHandler OnQuestObjectiveDeactivated;
		public event QuestCompleteEventHandler OnQuestComplete;
		public event QuestObjectiveCompleteEventHandler OnQuestObjectiveComplete;
		public event QuestObjectiveIncompleteEventHandler OnQuestObjectiveIncomplete;
		
			
		protected Dictionary<ulong, QuestObjectState> m_States = new Dictionary<ulong, QuestObjectState>(); // list of quest states specific to the player
		protected Dictionary<ulong, IQuestSystemObject> m_ActivelyChecked = new Dictionary<ulong, IQuestSystemObject>(); // list of actively checked quest system objects
		
		public QuestLog() {
		}
		
		public void QuestActivate(Quest quest) {
			QuestActivate(quest, 0);
		}

		public void QuestActivate(Quest quest, int inputPinIndex) {
			QuestActivate((QuestPin)quest.Inputs[inputPinIndex]);
		}

		public void QuestActivate(QuestPin pin) {
			if( pin.Node is Quest ) {
				GameDebugger.Log(LogLevel.Debug, "QS: Manually activating quest input pin '0x{0:X16}'", pin.Id);
				Activate(pin);
			}
		}
		
		public void QuestDeactivate(Quest quest) {
			Deactivate(quest);
			foreach( QuestPin pin in quest.Inputs )
				Deactivate(pin);
		}

		public void QuestComplete(Quest quest) {
			QuestComplete(quest, 0);
		}

		public void QuestComplete(Quest quest, int outputPinIndex) {
			QuestComplete((QuestPin)quest.Outputs[outputPinIndex]);
		}

		public void QuestComplete(QuestPin pin) {
			if( pin.Node is Quest ) {
				GameDebugger.Log(LogLevel.Debug, "QS: Manually completing quest '{0}' by activating pin '0x{1:X16}'", ((Quest)pin.Node).Name, pin.Id);
				Activate(pin);
			}
		}
		
		
		public void QuestObjectiveActivate(QuestObjective objective) {
			QuestObjectiveActivate(objective, 0);
		}

		public void QuestObjectiveActivate(QuestObjective objective, int inputPinIndex) {
			QuestObjectiveActivate((QuestPin)objective.Inputs[inputPinIndex]);
		}

		public void QuestObjectiveActivate(QuestPin pin) {
			if( pin.Node is QuestObjective ) {
				GameDebugger.Log(LogLevel.Debug, "QS: Manually activating objective input pin '0x{0:X16}'", pin.Id);
				Activate(pin);
			}
		}

		public void QuestObjectiveDeactivate(QuestObjective objective) {
			Deactivate(objective);
			foreach( QuestPin pin in objective.Inputs )
				Deactivate(pin);
		}
		
		public void QuestObjectiveComplete(QuestObjective objective) {
			QuestObjectiveComplete(objective, 0);
		}

		public void QuestObjectiveComplete(QuestObjective objective, int outputPinIndex) {
			QuestObjectiveComplete((QuestPin)objective.Outputs[outputPinIndex]);
		}

		public void QuestObjectiveComplete(QuestPin pin) {
			if( pin.Node is QuestObjective ) {
				GameDebugger.Log(LogLevel.Debug, "QS: Manually completing quest objective '{0}' by activating pin '0x{1:X16}'", ((QuestObjective)pin.Node).Name, pin.Id);
				Activate(pin);
			}
		}

		public void QuestObjectiveIncomplete(QuestObjective objective) {
			QuestObjectState state = GetState(objective);
			if( state.Locked || state.Completion == QuestObjectState.CompletionState.Incomplete )
				return;
			state.Completion = QuestObjectState.CompletionState.Incomplete;
		
			GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestObjectiveIncomplete");
			if( OnQuestObjectiveIncomplete != null )
				OnQuestObjectiveIncomplete.Invoke(new QuestObjectiveIncompleteEventArgs(objective));
			
			Deactivate((QuestPin)objective.Outputs[state.CompletionOutput]);
		}
		
		protected void Activate(QuestPin pin) {
			QuestObjectState state = GetState(pin);
			if( state.Locked || state.Active != QuestObjectState.ActivationState.Inactive )
				return;
			bool lockPin = false;
			if( pin.Type == GraphPin.PinType.Input ) {
				if( pin.Script != null ) {
					GameDebugger.Log(LogLevel.Debug, "QS: Partially activated pin '0x{0:X16}'", pin.Id);
					GameDebugger.Log(LogLevel.Debug, "QS: Adding pin '0x{0:X16}' to the list of actively checked objects", pin.Id);
					state.Active = QuestObjectState.ActivationState.PartiallyActive;
					m_ActivelyChecked.Add(pin.Id, pin);
				}
				else {
					state.Active = QuestObjectState.ActivationState.Active;
					GameDebugger.Log(LogLevel.Debug, "QS: Activated pin '0x{0:X16}'", pin.Id);
					if( pin.Node is Quest )
						Activate((BaseQuestObject)pin.Node);
					else
						CheckInputsOnActivation((BaseQuestObject)pin.Node);
				}
			}
			else {
				state.Active = QuestObjectState.ActivationState.Active;
				GameDebugger.Log(LogLevel.Debug, "QS: Activated pin '0x{0:X16}'", pin.Id);
				
				if( pin.Node is Quest || pin.Node is QuestObjective ) {
					QuestObjectState nodeState = GetState((IQuestSystemObject)pin.Node);
					int pinIndex = 0;
					foreach( QuestPin testPin in pin.Node.Outputs ) {
						if( testPin == pin ) {
							GameDebugger.Log(LogLevel.Debug, "QS: Detected pin index: {0}", pinIndex);
							break;
						}
						pinIndex++;
					}
					
					if( nodeState.Completion == QuestObjectState.CompletionState.Incomplete || nodeState.CompletionOutput != pinIndex ) {
						if( nodeState.Completion != QuestObjectState.CompletionState.Incomplete ) {
							Deactivate((QuestPin)pin.Node.Outputs[nodeState.CompletionOutput]);
							GameDebugger.Log(LogLevel.Debug, "QS: Switching {0} '0x{1:X16}' completion state from {2} to {3}", pin.Node.GetType().Name, ((IQuestSystemObject)pin.Node).Id, nodeState.CompletionOutput, pinIndex);
						}
						else {
							GameDebugger.Log(LogLevel.Debug, "QS: Setting {0} '0x{1:X16}' completion state to {2}", pin.Node.GetType().Name, ((IQuestSystemObject)pin.Node).Id, pinIndex);
						}
						nodeState.CompletionOutput = (byte)pinIndex;
						nodeState.Completion = pin.IsFailureOutput ? QuestObjectState.CompletionState.Failed : QuestObjectState.CompletionState.Successful;

						if( pin.Node is Quest ) {
							lockPin = true;
							GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestComplete");
							if( OnQuestComplete != null )
								OnQuestComplete.Invoke(new QuestCompleteEventArgs((Quest)pin.Node, pinIndex, pin.IsFailureOutput));
						}
						else {
							GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestObjectiveComplete");
							if( OnQuestObjectiveComplete != null )
								OnQuestObjectiveComplete.Invoke(new QuestObjectiveCompleteEventArgs((QuestObjective)pin.Node, pinIndex, pin.IsFailureOutput));
						}
					}
				}
				
				if( !(pin.Node is QuestObjective) ) // quest objectives have conditions instead of instructions on outputs
					ExecuteScript(pin);
			}
			
			// activate all outgoing connections
			if( state.Active == QuestObjectState.ActivationState.Active ) {
				foreach( QuestConnection connection in pin.Connections )
					if( connection.Source == pin )
						Activate(connection);
			}
			
			// locking the pin must be done in the end otherwise it will not activate connections (they will be already locked)
			if( lockPin )
				Lock(pin); 
		}
		
		protected void Activate(BaseQuestObject obj) {
			QuestObjectState state = GetState(obj);
			if( state.Locked || state.Active != QuestObjectState.ActivationState.Inactive )
				return;
			state.Active = QuestObjectState.ActivationState.Active;
			GameDebugger.Log(LogLevel.Debug, "QS: Activated {0} '0x{1:X16}'", obj.GetType().Name, obj.Id);
			if( obj is QuestCondition ) {
				// conditions must be actively checked every quest system tick 
				GameDebugger.Log(LogLevel.Debug, "QS: Adding {0} '0x{1:X16}' to the list of actively checked objects", obj.GetType().Name, obj.Id);
				m_ActivelyChecked.Add(obj.Id, obj);
			}
			else if( obj is QuestObjective ) {
				// quest objectives must be actively checked every quest system tick, but only when there is at least one output that has a script assigned
				bool hasScriptsOnOutputs = false;
				foreach( QuestPin pin in obj.Outputs ) {
					if( pin.Script != null ) {
						hasScriptsOnOutputs = true;
						break;
					}
				}
				if( hasScriptsOnOutputs ) {
					GameDebugger.Log(LogLevel.Debug, "QS: Adding {0} '0x{1:X16}' to the list of actively checked objects due to existing scripts on one or more outputs", obj.GetType().Name, obj.Id);
					m_ActivelyChecked.Add(obj.Id, obj);
				}
				GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestObjectiveActivated");
				if( OnQuestObjectiveActivated != null )
					OnQuestObjectiveActivated.Invoke(new QuestObjectiveActivatedEventArgs((QuestObjective)obj));
			}
			else if( obj is Quest ) {
				GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestActivated");
				if( OnQuestActivated != null )
					OnQuestActivated.Invoke(new QuestActivatedEventArgs((Quest)obj));
			}
			else {
				if( obj is QuestInstruction ) {
					ExecuteScript((QuestInstruction)obj);
				}
				foreach( QuestPin pin in obj.Outputs )
					Activate(pin);
			}
			// when quest gets activated it does nothing since objectives actually get activated by quest's active input pins
		}
		
		protected void Activate(QuestConnection connection) {
			QuestObjectState state = GetState(connection);
			if( state.Locked || state.Active != QuestObjectState.ActivationState.Inactive )
				return;
			state.Active = QuestObjectState.ActivationState.Active;
			GameDebugger.Log(LogLevel.Debug, "QS: Activated connection '0x{0:X16}'", connection.Id);
			Activate((QuestPin)connection.Target);
		}
		
		protected void Deactivate(QuestPin pin) {
			QuestObjectState state = GetState(pin);
			if( state.Locked || state.Active == QuestObjectState.ActivationState.Inactive )
				return;
			state.Active = QuestObjectState.ActivationState.Inactive;
			GameDebugger.Log(LogLevel.Debug, "QS: {0} '0x{1:X16}' deactivated", pin.GetType().Name, pin.Id);
			
			if( pin.Type == GraphPin.PinType.Input ) {
				if( m_ActivelyChecked.ContainsKey(pin.Id) ) {
					GameDebugger.Log(LogLevel.Debug, "QS: Removing {0} '0x{1:X16}' from the list of actively checked objects", pin.GetType().Name, pin.Id);
					m_ActivelyChecked.Remove(pin.Id);
				}
			}
			
			foreach( QuestConnection connection in pin.Connections ) {
				if( connection.Source != pin )
					continue;
				Deactivate(connection); 
			}
			
			if( pin.Type == GraphPin.PinType.Input ) {
				Deactivate((BaseQuestObject)pin.Node);
			}
		}
		
		protected void Deactivate(BaseQuestObject obj) {
			QuestObjectState state = GetState(obj);
			if( state.Locked || state.Active == QuestObjectState.ActivationState.Inactive )
				return;
			state.Active = QuestObjectState.ActivationState.Inactive;
			GameDebugger.Log(LogLevel.Debug, "QS: {0} '0x{1:X16}' deactivated", obj.GetType().Name, obj.Id);
			
			if( m_ActivelyChecked.ContainsKey(obj.Id) ) {
				GameDebugger.Log(LogLevel.Debug, "QS: Removing {0} '0x{1:X16}' from the list of actively checked objects", obj.GetType().Name, obj.Id);
				m_ActivelyChecked.Remove(obj.Id);
			}

			if( obj is Quest ) {
				GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestDeactivated");
				if( OnQuestDeactivated != null )
					OnQuestDeactivated.Invoke(new QuestDeactivatedEventArgs((Quest)obj));
				if( state.Completion != QuestObjectState.CompletionState.Incomplete ) {
					state.Completion = QuestObjectState.CompletionState.Incomplete;
					// NOTE: This most probably will never happen since quests are locked right after getting complete,
					// but might need to invoke OnQuestIncomplete event in case this happens after all.
				}
			}
			else if( obj is QuestObjective ) {
				GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestObjectiveDeactivated");
				if( OnQuestObjectiveDeactivated != null )
					OnQuestObjectiveDeactivated.Invoke(new QuestObjectiveDeactivatedEventArgs((QuestObjective)obj));
				if( state.Completion != QuestObjectState.CompletionState.Incomplete ) {
					state.Completion = QuestObjectState.CompletionState.Incomplete;
					GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestObjectiveIncomplete");
					if( OnQuestObjectiveIncomplete != null )
						OnQuestObjectiveIncomplete.Invoke(new QuestObjectiveIncompleteEventArgs((QuestObjective)obj));
				}
			}
			
			foreach( QuestPin pin in obj.Outputs )
				Deactivate(pin);
		}
		
		protected void Deactivate(QuestConnection connection) {
			QuestObjectState state = GetState(connection);
			if( state.Locked || state.Active == QuestObjectState.ActivationState.Inactive )
				return;
			state.Active = QuestObjectState.ActivationState.Inactive;
			GameDebugger.Log(LogLevel.Debug, "QS: {0} '0x{1:X16}' deactivated", connection.GetType().Name, connection.Id);
			
			Deactivate((QuestPin)connection.Target);
		}
		
		protected void Lock(QuestPin pin) {
			Lock(pin, true);
		}
		
		protected void Lock(QuestPin pin, bool propagate) {
			QuestObjectState state = GetState(pin);
			if( state.Locked )
				return;
			state.Locked = true;
			GameDebugger.Log(LogLevel.Debug, "QS: {0} '0x{1:X16}' locked", pin.GetType().Name, pin.Id);

			if( pin.Type == GraphPin.PinType.Input ) {
				if( m_ActivelyChecked.ContainsKey(pin.Id) ) {
					GameDebugger.Log(LogLevel.Debug, "QS: Removing {0} '0x{1:X16}' from the list of actively checked objects", pin.GetType().Name, pin.Id);
					m_ActivelyChecked.Remove(pin.Id);
				}
			}
			
			foreach( QuestConnection connection in pin.Connections ) {
				Lock(connection, (connection.Target == pin)); // propagate locking only on incoming connections 
			}
			
			if( propagate && pin.Type == GraphPin.PinType.Output ) {
				foreach( QuestPin otherPin in pin.Node.Outputs ) {
					if( otherPin == pin )
						continue;
					Lock(otherPin, false); 
				}
				Lock((BaseQuestObject)pin.Node);
			}
		}
		
		protected void Lock(BaseQuestObject obj) {
			QuestObjectState state = GetState(obj);
			if( state.Locked )
				return;
			state.Locked = true;
			GameDebugger.Log(LogLevel.Debug, "QS: {0} '0x{1:X16}' locked", obj.GetType().Name, obj.Id);
			
			if( m_ActivelyChecked.ContainsKey(obj.Id) ) {
				GameDebugger.Log(LogLevel.Debug, "QS: Removing {0} '0x{1:X16}' from the list of actively checked objects", obj.GetType().Name, obj.Id);
				m_ActivelyChecked.Remove(obj.Id);
			}
			
			foreach( QuestPin pin in obj.Inputs )
				Lock(pin, true);
		}

		protected void Lock(QuestConnection connection) {
			Lock(connection, true);
		}
		
		protected void Lock(QuestConnection connection, bool propagate) {
			QuestObjectState state = GetState(connection);
			if( state.Locked )
				return;
			state.Locked = true;
			GameDebugger.Log(LogLevel.Debug, "QS: {0} '0x{1:X16}' locked", connection.GetType().Name, connection.Id);
			if( propagate )
				Lock((QuestPin)connection.Source);
		}
		
		public void Tick() {
			QuestObjectState state;
			bool result;
			
			var values = m_ActivelyChecked.Values;
			IQuestSystemObject[] objects = new IQuestSystemObject[values.Count];
			values.CopyTo(objects, 0);
			
			GameDebugger.Log(LogLevel.Debug, "QS: TICK");
			foreach( IQuestSystemObject obj in objects ) {
				state = GetState(obj);
				if( state.Locked ) {
					GameDebugger.Log(LogLevel.Warning, "QS: {0} '0x{1:X16}' is in the list of actively checked objects while being locked", obj.GetType().Name, obj.Id);
					m_ActivelyChecked.Remove(obj.Id);
					continue;
				}
				GameDebugger.Log(LogLevel.Debug, "QS: Checking {0} '0x{1:X16}'", obj.GetType().Name, obj.Id);
				if( obj is QuestPin ) {
					QuestPin pin = (QuestPin)obj;
					result = CheckCondition(pin, true);
					if( result && state.Active == QuestObjectState.ActivationState.PartiallyActive ) {
						state.Active = QuestObjectState.ActivationState.Active;
						GameDebugger.Log(LogLevel.Debug, "QS: Switched {0} '0x{1:X16}' from partially activated to activated", obj.GetType().Name, obj.Id);
						if( pin.Node is Quest )
							Activate((BaseQuestObject)pin.Node);
						else
							CheckInputsOnActivation((BaseQuestObject)pin.Node);
						foreach( QuestConnection connection in pin.Connections )
							if( connection.Source == pin )
								Activate(connection);
					}
					else if( !result && state.Active == QuestObjectState.ActivationState.Active ) {
						state.Active = QuestObjectState.ActivationState.PartiallyActive;
						GameDebugger.Log(LogLevel.Debug, "QS: Switched {0} '0x{1:X16}' from activated to partially activated", obj.GetType().Name, obj.Id);
						Deactivate((BaseQuestObject)pin.Node);
						foreach( QuestConnection connection in pin.Connections )
							if( connection.Source == pin )
								Deactivate(connection);
					}
				}
				else if( obj is QuestCondition ) {
					// every tick depending on it's script execution result it switches active either output[0] or output[1] active 
					QuestCondition condition = (QuestCondition)obj;
					result = CheckCondition(condition, true);
					if( result ) {
						if( state.CompletionOutput != 0 || state.Completion == QuestObjectState.CompletionState.Incomplete ) {
							if( state.Completion != QuestObjectState.CompletionState.Incomplete ) {
								Deactivate((QuestPin)condition.Outputs[1]);
								GameDebugger.Log(LogLevel.Debug, "QS: Switching {0} '0x{1:X16}' output from FALSE to TRUE", obj.GetType().Name, obj.Id);
							}
							else {
								GameDebugger.Log(LogLevel.Debug, "QS: Setting {0} '0x{1:X16}' output to TRUE", obj.GetType().Name, obj.Id);
							}
							Activate((QuestPin)condition.Outputs[0]);
							state.CompletionOutput = 0;
						}
					}
					else {
						if( state.CompletionOutput != 1 || state.Completion == QuestObjectState.CompletionState.Incomplete ) {
							if( state.Completion != QuestObjectState.CompletionState.Incomplete ) {
								Deactivate((QuestPin)condition.Outputs[0]);
								GameDebugger.Log(LogLevel.Debug, "QS: Switching {0} '0x{1:X16}' output from TRUE to FALSE", obj.GetType().Name, obj.Id);
							}
							else {
								GameDebugger.Log(LogLevel.Debug, "QS: Setting {0} '0x{1:X16}' output to FALSE", obj.GetType().Name, obj.Id);
							}
							Activate((QuestPin)condition.Outputs[1]);
							state.CompletionOutput = 1;
						}
					}
					state.Completion = QuestObjectState.CompletionState.Successful;
				}
				else if( obj is QuestObjective ) {
					// every tick rechecks all conditions on output pins
					QuestObjective objective = (QuestObjective)obj;
					int pinIndex = 0;
					foreach( QuestPin pin in objective.Outputs ) {
						if( pin.Script == null ) {
							pinIndex++;
							continue;
						}
						result = CheckCondition(pin, false);
						if( result ) {
							Activate(pin); // this will also deactivate previously active pin
							break; // Just in case when there is more than one output that passes the check. We don't want lots of unneeded events, right?
						}
						if( pinIndex == state.CompletionOutput && state.Completion != QuestObjectState.CompletionState.Incomplete ) {
							state.Completion = QuestObjectState.CompletionState.Incomplete;
							GameDebugger.Log(LogLevel.Debug, "QS: event: OnQuestObjectiveIncomplete");
							if( OnQuestObjectiveIncomplete != null )
								OnQuestObjectiveIncomplete.Invoke(new QuestObjectiveIncompleteEventArgs(objective));
						}
						Deactivate(pin);
						pinIndex++;
					}
				}
				else {
					GameDebugger.Log(LogLevel.Warning, "QS: {0} '0x{1:X16}' is not supposed to be in the list of actively checked objects", obj.GetType().Name, obj.Id);
				}
			}
		}
		
		protected void CheckInputsOnActivation(BaseQuestObject obj) {
			// all objects except quests get activated when all their input pins are active
			bool allActive = true;
			GameDebugger.Log(LogLevel.Debug, "QS: Checking inputs of {0} '0x{1:X16}'", obj.GetType().Name, obj.Id);
			foreach( QuestPin pin in obj.Inputs ) {
				QuestObjectState state = GetState(pin);
				if( state.Active != QuestObjectState.ActivationState.Active ) {
					allActive = false;
					break;
				}
			}
			if( allActive ) {
				GameDebugger.Log(LogLevel.Debug, "QS: SUCCESS");
				Activate((BaseQuestObject)obj);
			}
			else {
				GameDebugger.Log(LogLevel.Debug, "QS: FAIL");
			}
		}
		
		public bool CheckCondition(IScriptableQuestSystemObject obj, bool defaultvalue) {
			Script script = obj.Script;
			GameDebugger.Log(LogLevel.Debug, "QS: Executing condition of {0} '0x{1:X16}'", obj.GetType().Name, obj.Id);
			if( script == null ) {
				GameDebugger.Log(LogLevel.Debug, "QS: no script to execute, result={0}", defaultvalue);
				return defaultvalue;
			}
			if( Player == null )
				GameDebugger.Log(LogLevel.Debug, "QS: WHERE DID THE PLAYER GO?!");
			dynamic result = script.Run(this);
			if( !(result is Boolean) ) {
				GameDebugger.Log(LogLevel.Warning, "Condition script exits with a non boolean result ({0}) on {1} '0x{2:X16}'", result.GetType().Name, obj.GetType().Name, obj.Id);
				return false;
			}
			GameDebugger.Log(LogLevel.Debug, "QS: done. result={0}", (bool)result);
			return (bool)result;
		}
		
		public void ExecuteScript(IScriptableQuestSystemObject obj) {
			Script script = obj.Script;
			GameDebugger.Log(LogLevel.Debug, "QS: Executing script of {0} '0x{1:X16}'", obj.GetType().Name, obj.Id);
			if( script == null ) {
				GameDebugger.Log(LogLevel.Debug, "QS: no script to execute");
				return;
			}
			script.Run(this);
			GameDebugger.Log(LogLevel.Debug, "QS: done");
		}
		
		public QuestObjectState GetState(IQuestSystemObject obj) {
			QuestObjectState state;
			if( m_States.TryGetValue(obj.Id, out state) )
				return state;
			state = new QuestObjectState();
			m_States.Add(obj.Id, state);
			return state;
		}
	}
	
	
	public delegate void QuestActivatedEventHandler(QuestActivatedEventArgs args);
	public delegate void QuestObjectiveActivatedEventHandler(QuestObjectiveActivatedEventArgs args);
	public delegate void QuestDeactivatedEventHandler(QuestDeactivatedEventArgs args);
	public delegate void QuestObjectiveDeactivatedEventHandler(QuestObjectiveDeactivatedEventArgs args);
	public delegate void QuestCompleteEventHandler(QuestCompleteEventArgs args);
	public delegate void QuestObjectiveCompleteEventHandler(QuestObjectiveCompleteEventArgs args);
	public delegate void QuestObjectiveIncompleteEventHandler(QuestObjectiveIncompleteEventArgs args);
	
	
	public class QuestActivatedEventArgs : EventArgs {
		public Quest Quest = null;
		public QuestActivatedEventArgs(Quest quest) {
			Quest = quest;
		}
	}
	
	public class QuestObjectiveActivatedEventArgs : QuestActivatedEventArgs {
		public QuestObjective Objective = null;
		public QuestObjectiveActivatedEventArgs(QuestObjective objective) : base((Quest)objective.Parent) {
			Objective = objective;
		}
	}
	
	public class QuestDeactivatedEventArgs : EventArgs {
		public Quest Quest = null;
		public QuestDeactivatedEventArgs(Quest quest) {
			Quest = quest;
		}
	}
	
	public class QuestObjectiveDeactivatedEventArgs : QuestDeactivatedEventArgs {
		public QuestObjective Objective = null;
		public QuestObjectiveDeactivatedEventArgs(QuestObjective objective) : base((Quest)objective.Parent) {
			Objective = objective;
		}
	}
	
	public class QuestCompleteEventArgs : EventArgs {
		public Quest Quest = null;
		public int CompletionIndex = 0;
		public bool IsFailure = false;
		public QuestCompleteEventArgs(Quest quest, int completionIndex, bool isFailure) {
			Quest = quest;
			CompletionIndex = completionIndex;
			IsFailure = isFailure;
		}
	}
	
	public class QuestObjectiveCompleteEventArgs : QuestCompleteEventArgs {
		public QuestObjective Objective = null;
		public QuestObjectiveCompleteEventArgs(QuestObjective objective, int completionIndex, bool isFailure) : base((Quest)objective.Parent, completionIndex, isFailure) {
			Objective = objective;
		}
	}

	public class QuestObjectiveIncompleteEventArgs : EventArgs {
		public Quest Quest = null;
		public QuestObjective Objective = null;
		public QuestObjectiveIncompleteEventArgs(QuestObjective objective) {
			Objective = objective;
			Quest = (Quest)objective.Parent;
		}
	}
}
