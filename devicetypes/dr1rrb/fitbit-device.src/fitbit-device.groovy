import static java.util.Calendar.*
import groovy.time.TimeCategory

/**
 *  Fitbit device
 *
 *  Copyright 2018 Dr1rrb
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
 *  in compliance with the License. You may obtain a copy of the License at:
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
 *  on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License
 *  for the specific language governing permissions and limitations under the License.
 *
 */
metadata {
	definition (name: "Fitbit device", namespace: "torick.net", author: "Dr1rrb") {
		capability "Sensor"
		capability "Sleep Sensor"
		capability "Step Sensor"
        
        capability "Actuator"
        capability "Refresh"

        attribute "nextAlarm", "DATE"
        attribute "estimatedTimeOfWake", "DATE"
		attribute "estimatedTimeOfSleep", "DATE"
        attribute "goaledTimeOfWake", "DATE"
		attribute "goaledTimeOfSleep", "DATE"
        
        attribute "_sleepSummary", "STRING"
        
        command "asleep"
        command "wakeUp"
	}


	simulator {
		// TODO: define status and reply messages here
	}

	tiles (scale: 2) {
        valueTile("main", "device.steps", width: 3, height: 2) {
            state "val", icon:'st.Health & Wellness.health11', label:'${currentValue}', defaultState: true
		}

		valueTile("stepsCount", "device.steps", width: 3, height: 2) {
            state "val", icon:'st.Health & Wellness.health11', label:'Steps:\r\n${currentValue}', defaultState: true
		}
        valueTile("stepsGoal", "device.goal", width: 3, height: 2) {
            state "val", icon:'st.Health & Wellness.health12', label:'Goal:\r\n${currentValue}', defaultState: true
		}
        
        standardTile("sleeping", "device.sleeping", width: 2, height: 2, decoration: "flat") {
        	state "not sleeping", icon:'st.Weather.weather14', backgroundColor: "#44b621", label:'awake', action: "asleep", nextState: "sleeping", defaultState: true
            state "sleeping", icon:'st.Weather.weather4', backgroundColor: "#153591", label:'sleeping', action: "wakeUp", nextState: "not sleeping"
		}
        valueTile("_sleepSummary", "device._sleepSummary", width: 4, height: 2) {
            state "val", label:'${currentValue}', defaultState: true
		}
        
        standardTile("refresh", "device.refresh", inactiveLabel: false, decoration: "flat", width: 2, height: 2) {
			state "default", action:"refresh.refresh", icon:"st.secondary.refresh"
        }
        
        main("main")
        details(["stepsCount", "stepsGoal", "sleeping", "_sleepSummary", "refresh"])
	}
}

def renew() { parent.authenticationRefresh() }

def installed() {
    log.debug "Installed with settings: ${settings}"
    configure()
}

def updated() {
	log.debug "Updated with settings: ${settings}"
	configure()
}

def refresh() { 
	log.debug "Refreshing"
	configure() 
}

def configure(){
	unschedule()
    
    parent.update()
	
    update()
    runEvery15Minutes(update)
}

def update() {
	try {
    	log.debug "Updating alarms (parent: ${parent})"
		def alarmsResponse = parent.makeGet("https://api.fitbit.com/1/user/-/devices/tracker/${device.deviceNetworkId}/alarms.json")
        updateAlarms(alarmsResponse.trackerAlarms)
    } catch(Exception e) {
    	log.error "Failed to update alarms: ${e}"
    }
}

def updateAlarms(alarms) {
    def nextDaysAlarms = alarms
    	.findAll { it.enabled && !it.deleted }
        .collectMany { toDateTimesForNextDays(it) }
        .sort()
	def nextAlarm = nextDaysAlarms.find()
    
    def currentNextAlarmState = device.currentState("nextAlarm").value
    def currentNextAlarm
    if (currentNextAlarmState) {
        currentNextAlarm = Date.parseToStringDate(currentNextAlarmState)
    }
    
    log.debug "Next alarm is : ${nextAlarm} while alarms for the next 7 days was: ${nextDaysAlarms}."
    
    // Notify anyway
    sendEvent(
        name: "nextAlarm", 
        value: nextAlarm,
        descriptionText: "${device.displayName} is now expecting to ring at ${nextAlarm}");
    
    if (currentNextAlarm != nextAlarm)
    {
    	// As the alarm changed, we may have to re-calculate the estimated***
        // So request a sleep udpate on parent, which will invoke local updateSleep, 
        // which will complete the udpate process (reSchedule + udpateSummary)
        parent.updateSleep()
    }
    else
    {
        reScheduleSleepEvents()
        updateSleepSummary()
    }
}

def updateActivities(activities) { // Maintained by parent
	log.debug "Updating activities: ${activities}"

 	sendEvent(
        name: "goal", 
        value: activities.goals.steps,
        descriptionText: "${device.displayName} steps goal is ${activities.goals.steps}");
	sendEvent(
        name: "steps", 
        value: activities.summary.steps,
        descriptionText: "${device.displayName} steps count is ${activities.summary.steps}");
}

def updateSleep(sleep) { // Maintained by parent
	log.debug "Updating sleep: ${sleep}"

	use( TimeCategory ) {
        def now = new Date()
        def tomorrow = now.next()
        def timeZone = (parent.state?.userProfile?.user?.timezone) ? TimeZone.getTimeZone(parent.state?.userProfile?.user?.timezone) : location.timeZone
		
        def nextAlarm, goaledWakeUp, goaledAsleep, expectedWakeUp, expectedAsleep

        def nextAlarmState = device.currentState("nextAlarm").value
        if (nextAlarmState) {
        	nextAlarm = Date.parseToStringDate(nextAlarmState)
        }
        
        if (sleep.goal?.wakeupTime) {
        	goaledWakeUp = new Date(Date.parse("HH:mm", sleep.goal.wakeupTime).time - timeZone.rawOffset)
            goaledWakeUp.set([year: now[YEAR], month: now[MONTH], date: now[DATE]])
        }
        if (sleep.goal?.bedtime) {
        	goaledAsleep = new Date(Date.parse("HH:mm", sleep.goal.bedtime).time - timeZone.rawOffset)
            goaledAsleep.set([year: now[YEAR], month: now[MONTH], date: now[DATE]])
        }
        
        if (nextAlarm && nextAlarm < tomorrow) {
        	log.debug "Alarm set within the next 24h, use it as wake up time: ${nextAlarm}"
            expectedWakeUp = nextAlarm
        } else if(sleep.consistency?.typicalWakeupTime) {
        	def typicalWakeUp = Date.parse("HH:mm", sleep.consistency?.typicalWakeupTime)
            typicalWakeUp.set([year: now[YEAR], month: now[MONTH], date: now[DATE]])

			log.debug "No alarm set for the next 24h. Fallback to the typical wake up: ${typicalWakeUp}"

            if (typicalWakeUp < now - 30.minutes) {
                expectedWakeUp = new Date(typicalWakeUp.next().time - timeZone.rawOffset)
            } else {
                expectedWakeUp = new Date(typicalWakeUp.time - timeZone.rawOffset)
            }
        } else {
        	expectedWakeUp = goaledWakeUp
        }
		
        if (expectedWakeUp) {
            expectedAsleep = expectedWakeUp - sleep.consistency.typicalDuration.minutes
        }
        
        log.debug "nextAlarm: ${nextAlarm}\r\ngoaledWakeUp: ${goaledWakeUp}\r\ngoaledAsleep: ${goaledAsleep}\r\nexpectedWakeUp: ${expectedWakeUp}\r\nexpectedAsleep: ${expectedAsleep}"

        sendEvent(
            name: "goaledTimeOfWake", 
            value: goaledWakeUp,
            descriptionText: "Wake up goal of ${device.displayName} is ${goaledWakeUp}");

        sendEvent(
            name: "goaledTimeOfSleep", 
            value: goaledAsleep,
            descriptionText: "Bedtime goal of ${device.displayName} is ${goaledAsleep}");

        sendEvent(
            name: "estimatedTimeOfWake", 
            value: expectedWakeUp,
            descriptionText: "${device.displayName} is now expecting to wake up at ${expectedWakeUp}");

        sendEvent(
            name: "estimatedTimeOfSleep", 
            value: expectedAsleep,
            descriptionText: "${device.displayName} is now expecting to go to sleep at ${expectedAsleep}");
	}
    
    reScheduleSleepEvents()
    updateSleepSummary()
}

def updateSleepSummary() {
	def nextAlarm = device.currentState("nextAlarm").value
	def expectedWakeUp = device.currentState("estimatedTimeOfWake").value
    def expectedAsleep = device.currentState("estimatedTimeOfSleep").value

	def summary = [];
    if (nextAlarm) {
    	def value = Date.parseToStringDate(nextAlarm)
        summary = summary << "Next alarm is set to ${value.format("HH:mm", location.timeZone)} on ${value.format("EEE dd", location.timeZone)}"
    }
    if (expectedAsleep) {
    	def value = Date.parseToStringDate(expectedAsleep)
        summary = summary << "Expected to sleep at ${value.format("HH:mm", location.timeZone)} on ${value.format("EEE dd", location.timeZone)}"
    }
    if (expectedWakeUp) {
    	def value = Date.parseToStringDate(expectedWakeUp)
        summary = summary << "Expected to wake at ${value.format("HH:mm", location.timeZone)} on ${value.format("EEE dd", location.timeZone)}"
    }

    if (summary.size() == 0) {
        summary = summary << "No data available"
    }
    sendEvent(
        name: "_sleepSummary", 
        value: summary.join("\r\n"),
        displayed: false);
}

def reScheduleSleepEvents() {
	unschedule(wakeUp)
    def wakeUpTime = device.currentState("estimatedTimeOfWake").value
    if (wakeUpTime) {
    	log.debug "Schedule wakeUp at ${wakeUpTime}"
        runOnce(Date.parseToStringDate(wakeUpTime), wakeUp)
    }

    unschedule(asleep)
    def asleepTime = device.currentState("estimatedTimeOfSleep").value
    if (asleepTime) {
    	log.debug "Schedule bedtime at ${asleepTime}"
        runOnce(Date.parseToStringDate(asleepTime), asleep)
    }
}

/// Commands
def wakeUp() { 
 	sendEvent(
        name: "sleeping", 
        value: "not sleeping",
        descriptionText: "${device.displayName} is now 'not sleeping'");
}

def asleep() {
 	sendEvent(
        name: "sleeping", 
        value: "sleeping",
        descriptionText: "${device.displayName} is now 'sleeping'");
}


/// Helpers
def toDateTimesForNextDays(alarm) {
	log.debug "Searching next occurences of alam: ${alarm}"

	def now = new Date()
	def dayOfWeek = now.format("u") as Integer
    
    def alarmTimeToday = Date.parse("HH:mmXXX", alarm.time)
    alarmTimeToday.set([year: now[YEAR], month: now[MONTH], date: now[DATE]])
    
    return alarm
    	.weekDays
        .collect { it
        	def i = dayIndex(it) 
            def delta = i - dayOfWeek
            
            // If the alarm day is before the current day of week, offset to next week
            if (delta < 0) {
            	delta += 7
            }
            
            def nextOccurence = alarmTimeToday.plus(delta)
            
            // If the alarm is for today, validate that it's not already in the past. If so offset by one week.
			// Note: we allow 30 min to wake up before offsetting by one week
            use( TimeCategory ) {
            if (nextOccurence < now - 30.minutes) {
            	nextOccurence = nextOccurence.plus(7)
            }}
            
            return nextOccurence
        }
}

def dayIndex(day) {
	switch(day) {
        case "MONDAY": return 1
        case "TUESDAY": return 2
        case "WEDNESDAY": return 3
        case "THURSDAY": return 4
        case "FRIDAY": return 5
        case "SATURDAY": return 6
        case "SUNDAY": return 7
    }
}

// parse events into attributes
def parse(String description) {
	log.debug "Parsing '${description}'"
}