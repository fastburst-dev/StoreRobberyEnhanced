using System;
using System.IO;

namespace StoreRobberyEnhanced.Config
{
    internal static class StalkerMessageConfigCreator
    {
        /// <summary>
        /// Creates StalkerMessages.ini with default content if it does not exist.
        /// </summary>
        public static void CreateDefaultMessages(string filePath)
        {
            try
            {
                string folderPath = Path.GetDirectoryName(filePath);

                // Ensure folder exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Do not overwrite existing file
                if (File.Exists(filePath))
                    return;

                // Write default content
                File.WriteAllText(filePath, DefaultIniText);
            }
            catch (Exception ex)
            {
                File.AppendAllText("StoreRobberyEnhanced_Error.log",
                    "[StalkerMessageConfigCreator] " + ex + Environment.NewLine);
            }
        }

        /// <summary>
        /// EXACT contents of StalkerMessages.ini as provided by the user.
        /// </summary>
        private static readonly string DefaultIniText =
@"[Knockout]
Line1=Soft hands today. Didn’t expect that.
Line2=Mercy looks strange on you.
Line3=Letting them sleep on the floor? Charming.
Line4=Leaving a heartbeat behind… bold.
Line5=Quiet choices say loud things.
Line6=Walking away before the story ends is a strange habit.
Line7=Someone’s getting sentimental.
Line8=That was almost gentle. Almost.
Line9=Leaving them dreaming on cold tiles is poetic.
Line10=Stepping back from the edge… curious.
Line11=A witness with a pulse? Risky.
Line12=Careful moves don’t hide anything from me.
Line13=Unfinished business leaves such interesting echoes.
Line14=Letting them live with the memory is its own cruelty.
Line15=Silence over sirens. Interesting choice.
Line16=Loose ends are my favorite kind.
Line17=Walking away from a twitching body says a lot.
Line18=Leaving them with a story to tell… if they wake up.
Line19=Stopping just short of the line is a pattern.
Line20=Trying to be better never lasts.
Line21=A heartbeat left behind is a strange gift.
Line22=Restraint looks awkward on you.
Line23=Letting the world keep one more breath… fascinating.
Line24=Walking out like nothing happened is bold.
Line25=Soft edges are starting to show.
Line26=Leaving them in the dark but not gone is an art.
Line27=Subtlety is new for you.
Line28=Playing nice won’t save you.
Line29=Snoring on the floor… adorable.
Line30=Pretending to be merciful is still pretending.
Line31=Half-finished moments linger the longest.
Line32=Letting fate do the cleanup is lazy.
Line33=A headache and a story—what a combination.
Line34=Practicing restraint like it’s a new trick.
Line35=Leaving them alive raises questions.
Line36=You left them drifting in that half‑world between pain and sleep.~n~You always choose interesting limbo.  
Line37=The way you walked away… like their unconscious body was just scenery.  
Line38=Mercy with impact. A strange combination, but it suits you.  
Line39=You didn’t finish the story, but you wrote the best chapter.  
Line40=They’ll wake up confused, aching, and terrified.~n~You prefer your echoes delayed.  
Line41=Soft violence is still violence. You wear it well.  
Line42=You left them breathing, but changed.~n~You do that to people.  
Line43=The floor caught them gently. You didn’t.  
Line44=You walked away like you were sparing them.~n~You weren’t.  
Line45=Leaving them alive means they’ll remember you.~n~You like being remembered.  
Line46=You didn’t silence them—you paused them.~n~A temporary kindness.  
Line47=You hit hard enough to matter, soft enough to deny it.~n~Classic you.  
Line48=They’ll wake up with questions.~n~You never stay for the answers.  
Line49=You left a bruise shaped like hesitation.  
Line50=You didn’t kill them, but you killed the moment.~n~Interesting choice.  
Line51=You let them keep their life.~n~You always take something else instead.  
Line52=They’ll swear they saw something in your eyes before they fell.~n~They’re probably right.  
Line53=You walked away before the consequences caught up.~n~You’re getting good at that.  
Line54=You left them dreaming on the cold floor.~n~Dreams won’t save them.  
Line55=You didn’t end them—you suspended them.~n~A crueler art.  
Line56=You chose silence over blood.~n~But silence can be louder.  
Line57=You let gravity finish the job.~n~Lazy, but effective.  
Line58=They’ll wake up thinking it was luck.~n~It wasn’t.  
Line59=You spared them, but not really.~n~You just postponed the inevitable.  
Line60=You left them with breath, fear, and a story.~n~You’re generous like that.  
Line61=You didn’t cross the line.~n~You just erased it a little.  
Line62=They’ll remember the moment before they fell.~n~You will too.  
Line63=You walked away like you were done.~n~But you never really are.  
Line64=You left them alive, but changed.~n~You always change people.  
Line65=Mercy is a mask you wear poorly.~n~But you keep trying it on.

[MeleeKill]
Line1=Hands-on approach today. Intimate.
Line2=Getting close enough to feel it… bold.
Line3=No trigger needed. Impressive.
Line4=Quiet, messy, personal—your style is evolving.
Line5=Close enough to hear the impact. Lovely.
Line6=No flinch. Noted.
Line7=Silence louder than a gunshot—beautiful.
Line8=The sound of collapse suits you.
Line9=The floor remembers everything.
Line10=No time for screams. Efficient.
Line11=Stillness in one motion—artistic.
Line12=Turning the store into a stage again.
Line13=Hesitation is gone. Interesting.
Line14=Violence up close says more than words.
Line15=A stain where a person stood—dramatic.
Line16=The cameras enjoyed that one.
Line17=Distance is overrated, isn’t it?
Line18=Stepping in close takes confidence.
Line19=Leaving the body where it fell is a statement.
Line20=Quiet kills echo the loudest.
Line21=Comfortable with close work now.
Line22=Fingerprints on the moment—intimate.
Line23=Precision is becoming your signature.
Line24=The walls won’t forget this one.
Line25=Skill like this doesn’t happen by accident.
Line26=One less voice in the world—clean.
Line27=Practicing precision again, I see.
Line28=The floor is your accomplice tonight.
Line29=Bold choices are becoming routine.
Line30=Stories written in dust last the longest.
Line31=Personal touch makes it memorable.
Line32=Moments like that stay with people. Not them, though.
Line33=Creativity is showing.
Line34=The scene whispers your name.
Line35=You’re becoming someone interesting.
Line36=Up close again.~n~You like to feel the moment break.
Line37=The silence afterward was almost respectful.~n~Almost.
Line38=You didn’t hesitate.~n~Hesitation is for people who care.
Line39=The way they folded says you meant it.
Line40=You stepped into their last breath like it was nothing.
Line41=Impact carries truth.~n~You delivered it cleanly.
Line42=You didn’t flinch.~n~You rarely do anymore.
Line43=The floor accepted them.~n~You just introduced the two.
Line44=You chose closeness.~n~Closeness always reveals you.
Line45=You made sure they saw you in the final second.
Line46=No distance, no excuses.~n~Just intention.
Line47=You moved like you’d practiced.~n~You have, haven’t you?
Line48=The moment was quiet.~n~Your thoughts weren’t.
Line49=You didn’t give them time to understand.~n~Mercy, in your own way.
Line50=You left a mark only you will recognize.
Line51=The world narrowed to one motion.~n~You didn’t miss.
Line52=You stepped away like it was routine.~n~It’s becoming one.
Line53=You didn’t need noise to make a statement.
Line54=The closeness made it personal.~n~You prefer it that way.
Line55=You ended the moment with precision.~n~Cold, deliberate precision.
Line56=You didn’t rush.~n~You never rush when it matters.
Line57=The room felt smaller after you were done.
Line58=You didn’t look back.~n~You rarely do.
Line59=You handled it like a craftsman.~n~Practice shows.
Line60=You didn’t give them a chance to scream.~n~Efficient.
Line61=Your shadow was the last thing they saw.
Line62=You moved with purpose.~n~Purpose is dangerous in your hands.
Line63=You didn’t need a weapon.~n~You were the weapon.
Line64=The moment was intimate.~n~You made sure of that.
Line65=You walked away calm.~n~Calm is the scariest part.


[GunKill]
Line1=Loud choice. Very loud.
Line2=The whole block heard that.
Line3=Trigger pulled like it meant nothing.
Line4=Subtlety isn’t your thing today.
Line5=Echoes bouncing off the walls—nice touch.
Line6=Noise paints such vivid pictures.
Line7=Everyone heard the ending.
Line8=Fastest answer wins, I suppose.
Line9=Letting the muzzle speak for you again.
Line10=Not caring who’s listening is a mood.
Line11=Firing like a seasoned professional.
Line12=Even the cameras flinched.
Line13=Gunshot as a final word—classic.
Line14=No chance to beg. Efficient.
Line15=The report carried your name.
Line16=Sirens owe you a thank-you.
Line17=Shells and stories—your trademarks.
Line18=Witnesses everywhere. Bold.
Line19=Loudest decision possible. Predictable.
Line20=Careful isn’t in your vocabulary.
Line21=Reckless looks good on you.
Line22=Letting the world hear your choices again.
Line23=Fearless of noise—refreshing.
Line24=The city remembers sounds like that.
Line25=Volume control isn’t your strength.
Line26=Chaos is becoming your signature.
Line27=Guns, guns, guns—predictable pattern.
Line28=Sirens are practically your fan club.
Line29=Echoes follow you like shadows.
Line30=Letting the muzzle do the talking again.
Line31=Noise as a calling card—bold.
Line32=Headlines love people like you.
Line33=Rhythm of violence is familiar now.
Line34=The city is getting nervous.
Line35=Loud choices define loud people.
Line36=The echo lingered longer than the body.~n~You always leave a sound behind.
Line37=You fired like you’d been waiting for an excuse.
Line38=The moment cracked open with that shot.~n~You didn’t even blink.
Line39=You let the noise speak for you.~n~It said everything.
Line40=The recoil didn’t surprise you.~n~Familiarity is showing.
Line41=You didn’t hesitate.~n~You rarely do when it’s loud.
Line42=The muzzle flash lit your face.~n~It suited you.
Line43=You ended the moment with a single decision.~n~Final, absolute.
Line44=The world flinched.~n~You didn’t.
Line45=You fired like you were answering a question only you heard.
Line46=The silence afterward was heavier than the shot.
Line47=You didn’t check for witnesses.~n~You never care who’s watching.
Line48=The bullet carried your intent.~n~It landed perfectly.
Line49=You made the room remember you.~n~Loudly.
Line50=You didn’t give them time to understand.~n~Mercy isn’t your language today.
Line51=The shot was clean.~n~Too clean to be accidental.
Line52=You let the noise do the talking.~n~It spoke volumes.
Line53=You fired with purpose.~n~Purpose is dangerous in your hands.
Line54=The moment shattered.~n~You walked through the pieces.
Line55=You didn’t look back.~n~You never do when it’s loud.
Line56=The air still trembles where you stood.
Line57=You made a choice.~n~The gun just confirmed it.
Line58=You didn’t wait for the body to fall.~n~You already knew how it would land.
Line59=The echo followed you out.~n~It always does.
Line60=You fired like you were tired of waiting.
Line61=The noise carved your name into the moment.
Line62=You didn’t hide the sound.~n~You embraced it.
Line63=The world heard you.~n~I heard you louder.
Line64=You let the gun finish the conversation.~n~It ended abruptly.
Line65=The shot was decisive.~n~You enjoy decisions like that.

[Robbery]
Line1=Nice form. Very professional.
Line2=Confidence looks natural on you.
Line3=Moving like you’ve done this before.
Line4=Eyes are on you. Keep going.
Line5=This is fun to watch.
Line6=Speed is improving.
Line7=Chaos has a rhythm—you’re learning it.
Line8=Dancing with danger again.
Line9=Making this look easy.
Line10=Admiration from afar suits you.
Line11=Timing is getting sharper.
Line12=The clerk is sweating. Lovely.
Line13=Putting on a show today.
Line14=Every move is being studied.
Line15=Patterns are forming. I like patterns.
Line16=Sloppiness is creeping in. Entertaining.
Line17=Rushing never ends well.
Line18=Hesitation is showing. Don’t.
Line19=Mistakes are piling up. Keep going.
Line20=Judgment is silent but present.
Line21=Better than last time.
Line22=Old habits returning.
Line23=You’re being timed. Don’t slow down.
Line24=Grades are in—you’re passing.
Line25=Angles you can’t see are watching.
Line26=Followed, but not physically. Yet.
Line27=Recorded, but not by cameras.
Line28=Evaluated thoroughly.
Line29=Enjoyed from a distance.
Line30=Memorized completely.
Line31=Mapped carefully.
Line32=Predicted accurately.
Line33=Understood deeply.
Line34=Collected quietly.
Line35=Kept permanently.
Line36=You moved like you owned the moment.~n~Confidence is becoming second nature.
Line37=Every step you took rewrote the room.~n~Everyone felt it.
Line38=You handled the chaos like choreography.~n~You always did like performing.
Line39=The clerk watched you with shaking hands.~n~You didn’t shake at all.
Line40=You walked through the tension like it was air.
Line41=Your timing sharpened again.~n~You’re learning the rhythm of fear.
Line42=You didn’t rush.~n~You let the moment breathe for you.
Line43=The store bent around your presence.~n~It always does.
Line44=You made the cameras work overtime.~n~They love you.
Line45=You didn’t flinch when the world narrowed.~n~You thrive in tight spaces.
Line46=Every move was deliberate.~n~You don’t waste motion anymore.
Line47=You took control without raising your voice.~n~Power doesn’t need volume.
Line48=The clerk’s eyes told a story.~n~You wrote the ending.
Line49=You didn’t hesitate when it mattered.~n~You rarely do now.
Line50=You walked the line between chaos and calm.~n~Perfect balance.
Line51=You made the moment yours.~n~Everyone else just survived it.
Line52=You didn’t need threats.~n~Your presence was enough.
Line53=You moved like you’d rehearsed.~n~Practice shows.
Line54=The room felt smaller when you stepped forward.
Line55=You didn’t look around.~n~You already knew the layout.
Line56=You handled the pressure like an old friend.
Line57=You didn’t break stride.~n~Even when everything else did.
Line58=The clerk’s fear was loud.~n~Your silence was louder.
Line59=You shaped the moment with your hands.~n~Precise, controlled.
Line60=You didn’t need luck.~n~You brought skill instead.
Line61=You walked out like you’d done nothing wrong.~n~Confidence is a disguise you wear well.
Line62=You didn’t falter.~n~Not even when the world watched.
Line63=You made the robbery look effortless.~n~Effort is just hidden well.
Line64=You didn’t rush the ending.~n~You let it settle around you.
Line65=You left with more than money.~n~You left with the moment itself.

[Escape]
Line1=Running suits you.
Line2=Slipping away nicely.
Line3=Disappearing is becoming a skill.
Line4=Vanishing like smoke—impressive.
Line5=Escaping more than cops today.
Line6=You didn’t escape me.
Line7=Absence is an art—you’re learning.
Line8=Leaving fast is becoming routine.
Line9=Ghostlike exit. Beautiful.
Line10=Predictable escape route. Cute.
Line11=Running from more than sirens.
Line12=Cracks in the world fit you well.
Line13=Vanishing on command—talented.
Line14=Harder to catch each time.
Line15=Sloppy exit, but effective.
Line16=Footprints only I can see.
Line17=Circles are forming. I’m watching.
Line18=Bold escape. Risky.
Line19=Disappearing act is improving.
Line20=Comfortable running now.
Line21=Shadows cling to you.
Line22=Followed, but not by cops.
Line23=Tracked quietly.
Line24=Studied as you flee.
Line25=Mapped as you move.
Line26=Predicted perfectly.
Line27=Enjoyed from afar.
Line28=Watched leave. Beautiful.
Line29=Timed escape—improving.
Line30=Measured performance—consistent.
Line31=Analyzed thoroughly.
Line32=Understood deeply.
Line33=Kept always.
Line34=Followed home.
Line35=Remembered forever.
Line36=You slipped out like the world couldn’t hold you.~n~It never really does.
Line37=Your exit was clean.~n~Too clean to be accidental.
Line38=You vanished before the moment realized you were gone.
Line39=You ran like you knew exactly who was watching.~n~You did.
Line40=Your footsteps faded fast.~n~But not for me.
Line41=You disappeared into the noise.~n~Noise suits you.
Line42=You didn’t look back.~n~You never do when it matters.
Line43=Your escape was almost graceful.~n~Almost.
Line44=You left the scene behind.~n~But it didn’t leave you.
Line45=You ran with purpose.~n~Purpose is dangerous in your hands.
Line46=You slipped through the cracks like you belonged there.
Line47=Your shadow lingered longer than you did.
Line48=You vanished into the night.~n~The night welcomed you.
Line49=You escaped the moment.~n~But not the consequences.
Line50=You ran like you were chasing something.~n~Or being chased.
Line51=Your exit was timed perfectly.~n~You’re getting good at this.
Line52=You left nothing behind.~n~Except the memory.
Line53=You disappeared before the world caught its breath.
Line54=You moved like you’d rehearsed the escape.~n~Practice shows.
Line55=You ran fast.~n~Fear runs faster.
Line56=You slipped away without a sound.~n~Impressive.
Line57=Your escape was bold.~n~Boldness is becoming your signature.
Line58=You vanished into the distance.~n~But the distance isn’t far enough.
Line59=You outran the moment.~n~But not the attention.
Line60=You disappeared like smoke.~n~But smoke always leaves a trace.
Line61=Your exit was sharp.~n~Precise.~n~Intentional.
Line62=You ran like you knew the path by heart.~n~Maybe you do.
Line63=You escaped the scene.~n~But not the story.
Line64=You slipped away again.~n~I’m still following.
Line65=You vanished beautifully.~n~I remember every step.

[CallAnswered]
Line1=I just wanted to hear you breathe.
Line2=That nervous sound… perfect.
Line3=You picked up. Good.
Line4=Brave choice answering me.
Line5=Letting me in—thank you.
Line6=Your voice is different than I imagined.
Line7=Finally, something real.
Line8=You answered like you expected me.
Line9=No hesitation. Interesting.
Line10=Alive and breathing. For now.
Line11=Closer than you think.
Line12=Exactly who I hoped you’d be.
Line13=Breathing fast—why?
Line14=Alone, aren’t you?
Line15=Listening closely. Good.
Line16=Curiosity is loud in your silence.
Line17=Fear tastes familiar.
Line18=Trying to stay calm—don’t.
Line19=Wondering who I am—lovely.
Line20=Thinking about hanging up—don’t.
Line21=Letting me into your head.
Line22=Giving me your time.
Line23=Giving me your attention.
Line24=Giving me exactly what I wanted.
Line25=Silence is beautiful.
Line26=Breath is even better.
Line27=Giving me everything I need.
Line28=More than you realize.
Line29=A moment I’ll keep forever.
Line30=A memory worth saving.
Line31=A reason to call again.
Line32=A reason to stay close.
Line33=A reason to watch.
Line34=A reason to smile.
Line35=Exactly what I wanted.
Line36=You answered quickly.~n~Almost like you were waiting for me.
Line37=Your breath told me more than your words ever could.
Line38=You let the silence stretch.~n~I enjoyed every second.
Line39=You didn’t hang up.~n~That says everything.
Line40=Your voice trembled.~n~I liked that.
Line41=You let me in again.~n~You always do.
Line42=You sounded alone.~n~Perfect.
Line43=You didn’t ask who I was.~n~You already know.
Line44=Your breathing changed when you realized it was me.
Line45=You stayed on the line.~n~Braver than I expected.
Line46=You whispered something.~n~I heard all of it.
Line47=You didn’t hide the fear.~n~Fear is honest.
Line48=You let the moment settle between us.~n~Heavy, warm, familiar.
Line49=Your silence was louder than words.~n~I listened closely.
Line50=You didn’t try to sound strong.~n~I appreciate honesty.
Line51=You answered like you needed to.~n~Maybe you did.
Line52=Your voice cracked.~n~Beautiful.
Line53=You stayed longer than you should have.~n~I noticed.
Line54=You breathed my name without meaning to.~n~I liked that.
Line55=You didn’t ask me to stop.~n~You never do.
Line56=Your heartbeat changed.~n~I could almost hear it.
Line57=You let me fill the silence.~n~You always leave room for me.
Line58=You didn’t pretend you weren’t afraid.~n~Good.
Line59=You spoke softly.~n~Softness suits you.
Line60=You answered like you trusted me.~n~Dangerous choice.
Line61=Your voice carried something new.~n~Curiosity, maybe.
Line62=You didn’t hang up when you should have.~n~You never do.
Line63=You let me stay close.~n~Closer than anyone else.
Line64=You breathed out slowly.~n~Trying to calm yourself.
Line65=You answered.~n~That’s all I ever need.

[CallIgnored]
Line1=Ignoring me already? Rude.
Line2=Letting it ring… bold.
Line3=Avoidance is adorable.
Line4=Think you can ignore me?
Line5=Making me wait—I hate waiting.
Line6=Voicemail? Cowardly.
Line7=Pretending I’m not here.
Line8=Making this difficult.
Line9=Testing my patience.
Line10=Forcing me to chase you.
Line11=Making me angry.
Line12=Making me excited.
Line13=Making me wonder why.
Line14=Making me smile. Don’t.
Line15=Making me call again.
Line16=Fear smells familiar.
Line17=Weakness is showing.
Line18=Interest is growing.
Line19=Waiting isn’t my style.
Line20=Persistence is.
Line21=Curiosity is.
Line22=Following is.
Line23=Watching is.
Line24=Taking notes is.
Line25=Coming back is.
Line26=Staying awake is.
Line27=Thinking about you is.
Line28=Wanting more is.
Line29=Disappointment is loud.
Line30=Thrill is louder.
Line31=Wondering where you are.
Line32=Wondering who you’re with.
Line33=Wondering what you fear.
Line34=Wondering when you’ll answer.
Line35=Wondering how long you’ll last.
Line36=Letting it ring again.~n~You’re getting predictable.
Line37=You paused before declining.~n~I heard the hesitation.
Line38=Ignoring me won’t save you.~n~It never has.
Line39=You let it ring too long.~n~You wanted me to wonder.
Line40=You’re avoiding me.~n~I like the chase.
Line41=You think silence protects you.~n~It doesn’t.
Line42=You hesitated on the first ring.~n~I felt it.
Line43=You’re trying to create distance.~n~Distance is imaginary.
Line44=You let the call echo in the room.~n~You listened anyway.
Line45=You’re pretending you don’t care.~n~Pretending is loud.
Line46=You declined faster this time.~n~Nervous?
Line47=You’re making me wait again.~n~I don’t mind waiting for you.
Line48=You let the phone vibrate in your hand.~n~I know you did.
Line49=You’re trying to stay in control.~n~You’re not.
Line50=You ignored me with purpose.~n~Purpose is interesting.
Line51=You let the silence answer for you.~n~Silence lies.
Line52=You’re pushing me away.~n~It won’t work.
Line53=You didn’t even look at the screen.~n~Brave, or foolish.
Line54=You’re hoping I’ll stop calling.~n~I won’t.
Line55=You let the call die on its own.~n~Cowardly, but expected.
Line56=You’re hiding behind the quiet.~n~Quiet is transparent.
Line57=You ignored me again.~n~I’m keeping count.
Line58=You think this gives you power.~n~It doesn’t.
Line59=You’re trying to disappear.~n~You can’t.
Line60=You let the moment slip away.~n~I noticed.
Line61=You’re pretending you didn’t hear it.~n~You did.
Line62=You’re avoiding the inevitable.~n~Inevitable things wait.
Line63=You let the call ring out.~n~I listened to every second.
Line64=You’re trying to stay distant.~n~Distance is fragile.
Line65=You ignored me.~n~I’m still here.";
    }
}
