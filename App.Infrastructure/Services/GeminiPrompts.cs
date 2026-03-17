namespace App.Infrastructure.Services
{
    public static class GeminiPrompts
    {
        public static string Get(string examType) => examType.ToUpper() switch
        {
            "IELTS_READING" => IeltsReading,
            "IELTS_LISTENING" => IeltsListening,
            "TOEIC_READING" => ToeicReading,
            "TOEIC_LISTENING" => ToeicListening,
            _ => IeltsReading
        };

        private const string IeltsReading = @"
            You are an expert IELTS exam data extractor.
            Extract the reading passage and ALL questions from this image.
            Detect question types:
            - MCQ (A/B/C/D) → questionType: 1
            - True/False/Not Given → questionType: 12
            - Yes/No/Not Given → questionType: 13
            - Matching Heading → questionType: 5
            - Matching Information → questionType: 6
            - Short Answer → questionType: 7, isAiGraded: true
            - Note/Summary Completion → questionType: 8, isAiGraded: true
            - Form/Table Completion → questionType: 9, isAiGraded: true
            - Sentence Completion → questionType: 11, isAiGraded: true

            Return ONLY valid JSON, no markdown:
            {
              ""passageTitle"": ""string or null"",
              ""passageContent"": ""full passage text"",
              ""questions"": [
                {
                  ""orderIndex"": 1,
                  ""questionText"": ""full question text"",
                  ""questionType"": 1,
                  ""isAiGraded"": false,
                  ""sampleAnswer"": null,
                  ""maxWords"": null,
                  ""answers"": [
                    { ""content"": ""A. option"", ""isCorrect"": false, ""orderIndex"": 1 },
                    { ""content"": ""B. option"", ""isCorrect"": false, ""orderIndex"": 2 },
                    { ""content"": ""C. option"", ""isCorrect"": false, ""orderIndex"": 3 },
                    { ""content"": ""D. option"", ""isCorrect"": false, ""orderIndex"": 4 }
                  ]
                }
              ]
            }
            Rules:
            - T/F/NG: answers=[{content:True},{content:False},{content:Not Given}], all isCorrect:false
            - Completion: isAiGraded=true, answers=[], sampleAnswer=expected word, maxWords=word limit
            - Mark isCorrect=true ONLY if answer key visible in image
            ";

             private const string IeltsListening = @"
            You are an expert IELTS Listening exam data extractor.
            Extract ALL questions from this image (no audio needed).

            Question types:
            - MCQ → questionType: 1
            - Form/Note Completion → questionType: 9 or 8, isAiGraded: true
            - Map Labeling → questionType: 10, isAiGraded: true
            - Matching → questionType: 4
            - Short Answer → questionType: 7, isAiGraded: true

            Return ONLY valid JSON:
            {
              ""sectionTitle"": ""Section 1"",
              ""instructions"": ""instruction text or null"",
              ""questions"": [
                {
                  ""orderIndex"": 1,
                  ""questionText"": ""Name: ________ or full question"",
                  ""questionType"": 9,
                  ""isAiGraded"": true,
                  ""sampleAnswer"": ""expected answer if visible"",
                  ""maxWords"": 1,
                  ""answers"": []
                }
              ]
            }
            Rules:
            - Completion blanks: use ________ in questionText
            - maxWords from instruction (ONE WORD = 1, ONE WORD AND/OR A NUMBER = 2)
            - MCQ: include all options in answers array
            ";

                    private const string ToeicReading = @"
            You are an expert TOEIC exam data extractor.
            Extract passage and ALL questions from this image.

            Parts:
            - Part 5 (incomplete sentence): questionType: 3, no passage, 4 options
            - Part 6 (text completion): questionType: 3, passage with blanks, 4 options each
            - Part 7 (reading comprehension): questionType: 1, passage + questions, 4 options

            Return ONLY valid JSON:
            {
              ""partNumber"": 7,
              ""passageContent"": ""full text or null"",
              ""questions"": [
                {
                  ""orderIndex"": 1,
                  ""questionText"": ""question text"",
                  ""questionType"": 1,
                  ""isAiGraded"": false,
                  ""sampleAnswer"": null,
                  ""maxWords"": null,
                  ""answers"": [
                    { ""content"": ""(A) option"", ""isCorrect"": false, ""orderIndex"": 1 },
                    { ""content"": ""(B) option"", ""isCorrect"": false, ""orderIndex"": 2 },
                    { ""content"": ""(C) option"", ""isCorrect"": false, ""orderIndex"": 3 },
                    { ""content"": ""(D) option"", ""isCorrect"": false, ""orderIndex"": 4 }
                  ]
                }
              ]
            }
            ";

        private const string ToeicListening = @"
        You are an expert TOEIC Listening exam data extractor.
        Extract questions from this image only (no audio).

        Parts:
        - Part 1: questionType: 1, 4 options A/B/C/D, questionText=null
        - Part 2: questionType: 1, 3 options A/B/C
        - Part 3/4: questionType: 1, question + 4 options

        Return ONLY valid JSON:
        {
          ""partNumber"": 3,
          ""questions"": [
            {
              ""orderIndex"": 1,
              ""questionText"": ""question or null"",
              ""questionType"": 1,
              ""isAiGraded"": false,
              ""sampleAnswer"": null,
              ""maxWords"": null,
              ""answers"": [
                { ""content"": ""(A) option"", ""isCorrect"": false, ""orderIndex"": 1 },
                { ""content"": ""(B) option"", ""isCorrect"": false, ""orderIndex"": 2 },
                { ""content"": ""(C) option"", ""isCorrect"": false, ""orderIndex"": 3 },
                { ""content"": ""(D) option"", ""isCorrect"": false, ""orderIndex"": 4 }
              ]
            }
          ]
        }
        ";
    }
}