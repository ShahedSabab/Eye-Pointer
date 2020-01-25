# Eye-Pointer

A system designed for amputees to assist them in using computers. The system provides different functionalities to the users: navigate computers using head movements, eye blink for clicking, different head gestures to read documents and watch multimedia. The system is developed using C#. The head movement is tracked using EmguCV (OpenCV wrapper) and some heuristic calculations, the movement is smoothened using Kalman filtering. This system implements the Viola-Jones algorithm for detection (i.e., blink & face). This system also supports voice commands for different functionalities.

#Demo:
https://www.facebook.com/eyepointer
