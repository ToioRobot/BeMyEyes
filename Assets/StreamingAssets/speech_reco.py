import speech_recognition as sr
import pathlib

r = sr.Recognizer()

note = sr.AudioFile(str(pathlib.Path(__file__).parent.absolute()) + "/audio.wav")
with note as source:
    audio = r.record(source)

say = r.recognize_google(audio, language="it-IT")

print(u''.join(say).encode('utf-8'))
