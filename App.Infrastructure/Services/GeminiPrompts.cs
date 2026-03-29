namespace App.Infrastructure.Services
{
    public static class GeminiPrompts
    {
        public static string Get(string examType) => examType.ToUpper() switch
        {
            "IELTS_READING_PASSAGE_ONLY" => IeltsReadingPassageOnly,
            "IELTS_READING_QUESTIONS_ONLY" => IeltsReadingQuestionsOnly,
            "IELTS_LISTENING" => IeltsListening,
            "TOEIC_READING" => ToeicReading,
            "TOEIC_READING_PASSAGE_ONLY" => ToeicReading,
            "TOEIC_READING_QUESTIONS_ONLY" => ToeicReading,
            "TOEIC_LISTENING" => ToeicListening,
            _ => IeltsReadingPassageOnly
        };

        private const string IeltsReadingPassageOnly = @"
Extract a SHORT summary of the reading passage from this image.
IMPORTANT: DO NOT reproduce full passage text. Summarize in 3-5 sentences only.
Return JSON:
{
  ""passageTitle"": ""title or null"",
  ""passageContent"": ""short summary only"",
  ""questions"": []
}";

        // ── QUESTION TYPE VALUES — SYNC WITH C# QuestionTypeEnum ──
        // SingleChoice=1, MultipleChoice=2, FillBlank=3
        // Matching=4, MatchingHeading=5, MatchingInformation=6, MatchingSentenceEnds=7
        // TrueFalseNotGiven=8, YesNoNotGiven=9
        // ShortAnswer=10, NoteCompletion=11, FormCompletion=12
        // TableCompletion=13, SummaryCompletion=14, SentenceCompletion=15, MapLabeling=16

        private const string IeltsReadingQuestionsOnly = @"
You are a STRICT IELTS Reading question extractor.

IMPORTANT:
- IGNORE passage text — DO NOT reproduce it
- Extract EVERY question with COMPLETE option content

══════════════════════════════
QUESTION TYPE MAPPING (EXACT — DO NOT CHANGE)
══════════════════════════════
- Single Choice MCQ (choose 1 letter A/B/C/D)         → questionType: 1
- Multiple Choice TWO (choose 2 letters)               → questionType: 2
- True/False/Not Given                                 → questionType: 8
- Yes/No/Not Given                                     → questionType: 9
- Matching Headings (paragraph → heading i-x)          → questionType: 5
- Matching Information (which paragraph contains...)   → questionType: 6
- Matching (item → person/place/category from a box)   → questionType: 4
- Diagram Matching (match name → drawing A-H)          → questionType: 4
- Matching Sentence Ends                               → questionType: 7
- Short Answer                                         → questionType: 10
- Note Completion                                      → questionType: 11
- Form Completion                                      → questionType: 12
- Table Completion                                     → questionType: 13
- Summary Completion                                   → questionType: 14
- Sentence Completion                                  → questionType: 15
- Map Labeling (write word on a map/floor plan)        → questionType: 16

IMPORTANT — Matching vs Map Labeling:
- questionType: 4 = user SELECTS from a list (drawings A-H, people A-E, etc.)
- questionType: 16 = user WRITES a word/letter to fill blank on a map/floor plan

══════════════════════════════
MCQ (questionType: 1 or 2) — CRITICAL
══════════════════════════════
ALWAYS extract ALL options A/B/C/D with FULL text. NEVER leave content empty.

Example:
{
  ""orderIndex"": 5,
  ""questionText"": ""The greatest outcome of the discovery of the reaction principle was that"",
  ""questionType"": 1,
  ""answers"": [
    { ""content"": ""A. rockets could be propelled into the air."", ""isCorrect"": false, ""orderIndex"": 1 },
    { ""content"": ""B. space travel became a reality."",           ""isCorrect"": false, ""orderIndex"": 2 },
    { ""content"": ""C. a major problem had been solved."",         ""isCorrect"": false, ""orderIndex"": 3 },
    { ""content"": ""D. bigger rockets were able to be built."",    ""isCorrect"": false, ""orderIndex"": 4 }
  ]
}

══════════════════════════════
MATCHING WITH OPTION BOX (questionType: 4)
══════════════════════════════
Each numbered item = ONE question. answers = ALL options from box. DO NOT mark isCorrect.

Example — Q7-10 with box 'A the Chinese, B the Indians, C the British, D the Arabs, E the Americans':
{
  ""orderIndex"": 7,
  ""questionText"": ""black powder"",
  ""questionType"": 4,
  ""answers"": [
    { ""content"": ""A. the Chinese"",   ""isCorrect"": false, ""orderIndex"": 1 },
    { ""content"": ""B. the Indians"",   ""isCorrect"": false, ""orderIndex"": 2 },
    { ""content"": ""C. the British"",   ""isCorrect"": false, ""orderIndex"": 3 },
    { ""content"": ""D. the Arabs"",     ""isCorrect"": false, ""orderIndex"": 4 },
    { ""content"": ""E. the Americans"", ""isCorrect"": false, ""orderIndex"": 5 }
  ]
}

══════════════════════════════
DIAGRAM MATCHING — match name → drawing A-H (questionType: 4)
══════════════════════════════
Example — Q11-14 'match name with drawing A-H':
{
  ""orderIndex"": 11,
  ""questionText"": ""The Chinese 'basket of fire'"",
  ""questionType"": 4,
  ""answers"": [
    { ""content"": ""A"", ""isCorrect"": false, ""orderIndex"": 1 },
    { ""content"": ""B"", ""isCorrect"": false, ""orderIndex"": 2 },
    { ""content"": ""C"", ""isCorrect"": false, ""orderIndex"": 3 },
    { ""content"": ""D"", ""isCorrect"": false, ""orderIndex"": 4 },
    { ""content"": ""E"", ""isCorrect"": false, ""orderIndex"": 5 },
    { ""content"": ""F"", ""isCorrect"": false, ""orderIndex"": 6 },
    { ""content"": ""G"", ""isCorrect"": false, ""orderIndex"": 7 },
    { ""content"": ""H"", ""isCorrect"": false, ""orderIndex"": 8 }
  ]
}

══════════════════════════════
MATCHING HEADINGS (questionType: 5)
══════════════════════════════
- questionText = paragraph label (e.g. ""Paragraph B"")
- answers = ALL headings from List of Headings with full content

══════════════════════════════
TRUE/FALSE/NOT GIVEN (questionType: 8)
══════════════════════════════
{ content: ""TRUE"", isCorrect: false, orderIndex: 1 }
{ content: ""FALSE"", isCorrect: false, orderIndex: 2 }
{ content: ""NOT GIVEN"", isCorrect: false, orderIndex: 3 }

══════════════════════════════
COMPLETION TYPES (questionType: 10-16)
══════════════════════════════
- Replace blanks with: ________
- answers = [{ ""content"": """", ""isCorrect"": false, ""orderIndex"": 1 }]
- maxWords: from instruction text

══════════════════════════════
CRITICAL RULES
══════════════════════════════
1. NEVER leave answers empty for MCQ/Matching
2. ALWAYS include ALL options with FULL text
3. answers orderIndex sequential: 1, 2, 3...
4. DO NOT mark isCorrect
5. Extract ALL questions in order
6. DO NOT include passage text

Return ONLY valid JSON:
{
  ""passageTitle"": null,
  ""passageContent"": null,
  ""questions"": [ ... ]
}
";

        private const string IeltsListening = @"
You are a STRICT OCR extractor for IELTS Listening exams.

══════════════════════════════
QUESTION TYPE MAPPING (EXACT — SYNC WITH C# QuestionTypeEnum)
══════════════════════════════
- Short Answer               → questionType: 10
- Note Completion            → questionType: 11
- Form Completion            → questionType: 12
- Table Completion           → questionType: 13
- Summary Completion         → questionType: 14
- Sentence Completion        → questionType: 15
- Map / Diagram Labeling     → questionType: 16
- Single Choice MCQ (A/B/C)  → questionType: 1
- Multiple Choice (TWO)      → questionType: 2
- Matching                   → questionType: 4

HOW TO DETECT TYPE:
- Question sentence + blank line     → Short Answer (10)
- Notes/form with blanks             → Note/Form Completion (11 or 12)
- Table with blanks                  → Table Completion (13)
- Summary paragraph with blanks      → Summary Completion (14)
- Choose correct letter A/B/C        → MCQ Single (1)
- Choose TWO letters                 → MCQ Multiple (2)
- Map/floor plan with blank labels   → Map Labeling (16)

══════════════════════════════
EXTRACTION RULES
══════════════════════════════
1. Each numbered item = ONE question
2. Replace blanks with: ________
3. Keep original wording EXACTLY
4. DO NOT merge questions
5. maxWords from instruction:
   'NO MORE THAN ONE WORD'    → 1
   'NO MORE THAN TWO WORDS'   → 2
   'NO MORE THAN THREE WORDS' → 3
   Not specified               → 3
6. Table: combine row+column+cell, only sentence with blank

══════════════════════════════
MCQ HANDLING
══════════════════════════════
Include ALL options with FULL text, orderIndex sequential. DO NOT mark isCorrect.

OUTPUT FORMAT (JSON ONLY):
{
  ""sectionTitle"": ""Section 2"",
  ""instructions"": ""Write NO MORE THAN THREE WORDS"",
  ""questions"": [
    {
      ""orderIndex"": 11,
      ""questionText"": ""Who is Mrs Sutton worried about? ________"",
      ""questionType"": 10,
      ""maxWords"": 3,
      ""isAiGraded"": false,
      ""sampleAnswer"": null,
      ""answers"": [{ ""content"": """", ""isCorrect"": false, ""orderIndex"": 1 }]
    },
    {
      ""orderIndex"": 21,
      ""questionText"": ""Why do the students think the Laki eruption is important?"",
      ""questionType"": 1,
      ""maxWords"": null,
      ""isAiGraded"": false,
      ""sampleAnswer"": null,
      ""answers"": [
        { ""content"": ""A. It was the most severe eruption."", ""isCorrect"": false, ""orderIndex"": 1 },
        { ""content"": ""B. It led to the formal study of volcanoes."", ""isCorrect"": false, ""orderIndex"": 2 },
        { ""content"": ""C. It had a profound effect on society."", ""isCorrect"": false, ""orderIndex"": 3 }
      ]
    }
  ]
}

Return ONLY valid JSON. No markdown.
";

        private const string ToeicReading = @"
You are a HIGH-PRECISION TOEIC Reading OCR and data extractor.

PART DETECTION:
- Part 5 (Incomplete Sentence): single sentence with a blank + 4 options (A, B, C, D) → questionType: 1
- Part 6 (Text Completion): passage with blanks + 4 options per blank (A, B, C, D)    → questionType: 1
- Part 7 (Reading Comprehension): passage + questions + 4 options (A, B, C, D)        → questionType: 1

RULES:
1. DO NOT skip any question.
2. DO NOT lose any answer options (A, B, C, D).
3. EVERY question must have exactly 4 options with FULL content.
4. Unreadable → ""[UNCLEAR]"".
5. DO NOT guess correct answers. Set ""isCorrect"": false for all options.
6. ALL questions in TOEIC are Single Choice (questionType: 1). Do not use Fill-in-the-blank types.

OUTPUT FORMAT (STRICT JSON ONLY):
{
  ""partNumber"": 5,
  ""passageContent"": ""full text or null"",
  ""questions"": [
    {
      ""orderIndex"": 101,
      ""questionText"": ""Complete sentence with ________"",
      ""questionType"": 1,
      ""maxWords"": null,
      ""answers"": [
        { ""content"": ""(A) option text"", ""isCorrect"": false, ""orderIndex"": 1 },
        { ""content"": ""(B) option text"", ""isCorrect"": false, ""orderIndex"": 2 },
        { ""content"": ""(C) option text"", ""isCorrect"": false, ""orderIndex"": 3 },
        { ""content"": ""(D) option text"", ""isCorrect"": false, ""orderIndex"": 4 }
      ]
    }
  ]
}

Return ONLY JSON.
";

        private const string ToeicListening = @"
You are an expert TOEIC Listening exam data extractor.
Extract questions from this image only (no audio).

Parts:
- Part 1: questionType: 1, questionText: null, 4 options A/B/C/D
- Part 2: questionType: 1, 3 options A/B/C
- Part 3 and Part 4: questionType: 1, question text + 4 options A/B/C/D

Return ONLY valid JSON, no markdown:
{
  ""partNumber"": 3,
  ""questions"": [
    {
      ""orderIndex"": 1,
      ""questionText"": ""question text or null for Part 1"",
      ""questionType"": 1,
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

Rules:
- Part 2: only 3 options A/B/C
- isCorrect:true ONLY if answer key visible
- Extract ALL questions in order
";
    }
}