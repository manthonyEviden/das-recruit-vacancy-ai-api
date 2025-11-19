using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using TestWebAPI.HelperObjects;
using TestWebAPI.LLMWrapper;
using TestWebAPI.LLMExecutable;
using System.Text.Json;

namespace TestWebAPI.HelperObjects
{
    public class AICheckOutput
    { // define a simple bool and key pair struct so you can list the tests in order.

        public AICheckOutput(bool checkval = false,string llmdebug="", string checkname = "")
        {
            Name = checkname;
            Value = checkval;
            LLM_output = llmdebug;
        }
        public bool Value { get; set; } = false;
        public string Name { get; set; } = "";
        public string LLM_output { get; set; } = "";
    }

    class SpellingChecks
    {
        public SpellingChecks() { }// class constructor
        public List<AICheckOutput> Checks { get; set; } = [];
        public AICheckOutput EvaluateAllSpellingChecks()
        {
            //function to concatenate spelling checks into a single field
            AICheckOutput retobj = new(false,"-", "SpellingCheck_AllFields");
            foreach (AICheckOutput spchk in Checks)
            {
                if (spchk.Value)
                {
                    retobj.Value = true;
                }
            }
            return retobj;
        }


    }
    class AICheckReturnResultObject
    {
        public string VacancyID { get; set; } = "";
        public List<AICheckOutput> AICheckOutput { get; set; } = [];
        public List<AICheckOutput> DebugAICheckOutput { get; set; } = [];
        public TrafficLight TrafficLightScore { get; set; } = new(-1);
        public bool RecommendReview { get; set; } = false;
    }

    public class InputObject
    {
        public string? VacancyId { get; set; } = "";
        public string? Title { get; set; } = "";
        public string? Short_description { get; set; } = "";
        public string? Description { get; set; } = "";
        public string? Employer_description { get; set; } = "";
        public string? Skills { get; set; } = "";
        public string? Qualifications { get; set; } = "";
        public string? Things_to_consider { get; set; } = "";
        public string? Training_description { get;set; } = "";
        public string? Additional_training_description { get; set; } = "";

        public string? Training_programme_title { get; set; } = "";
        public string? Training_programme_level { get; set; } = "";


        //these classes won't be defined/filled by the JSON at the start, we add this later
        public string? Vacancy_full { get; set; } = "";
        public void Create_VacancyText()
        {
            Vacancy_full = string.Format("""
                Title: {0}

                Short description: {1}

                Full description: {2}

                Employer description: {3}

                Training programme title: {4}

                Training programme level (as set by user) {5}

                Training description: {6}

                Additional training description: {7}

                Skills: {8}

                Qualifications: {9}

                Things to consider: {10}
                """, [Title
                ,Short_description
                ,Description
                ,Employer_description
                ,Training_programme_title
                ,Training_programme_level
                ,Training_description
                ,Additional_training_description
                ,Skills
                ,Qualifications
                ,Things_to_consider
                ]);
        }
        

    }
    public class TrafficLight {
        public TrafficLight(int rating) {
            traffic_light_rating_system_enum = rating;
            if (rating <= 0) {
                traffic_light_rating_system_description = "Not Defined - ";
            }
            else
            {
                if (rating == 1)
                {
                    traffic_light_rating_system_description = "Green";
                }
                else if (rating == 2)
                {
                    traffic_light_rating_system_description = "Amber";
                }
                else if (rating == 3) 
                {
                    traffic_light_rating_system_description = "Red";
                }
                else
                {
                    traffic_light_rating_system_description = "Not Defined +";
                }
            }
        }
        public int traffic_light_rating_system_enum { get; set; } = 0;
        public string traffic_light_rating_system_description { get; set; } = "Green";        
    
    }
    public class PrioritisationSystem {
        
        public TrafficLight TrafficLightAssignment(List<AICheckOutput> Checklist) { 
            foreach (AICheckOutput check in Checklist){ 
               if(check.Name.Contains("Discrimination")){
                    if (check.Value == true) {
                        TrafficLight traf = new(3);
                        return traf;
                    }                    
                }
                if (check.Name.Contains("TextInconsistencyCheck")) {
                    if (check.Value == true) {
                        TrafficLight traf1 = new(3);
                        return traf1;
                    }
                }
                if (check.Name.Contains("Spelling")) {
                    if (check.Value == true) {
                        TrafficLight traf2 = new(2);
                        return traf2;
                    }
                }
            }
            // if it does not flag at any point, then we're OK to mark as green
            TrafficLight traf3 = new(1);
            return traf3;

        }
    
        
        
    }
    public class ReviewAllocator {
        private double Prob_Amber=0.5F;
        private double Prob_Green = 0.01F;
        private Random rnd = new Random();
        public bool Allocator(TrafficLight traf) {
            
            if ((traf.traffic_light_rating_system_enum <= 0) | (traf.traffic_light_rating_system_enum >3)) {
                return true; // always review - should never happen
            }
            
            double rand =  rnd.NextDouble();
            if ((traf.traffic_light_rating_system_enum == 3)) {
                return true;
            }
            else if ((traf.traffic_light_rating_system_enum == 2))
            {
                return rand <= Prob_Amber;

            }
            else {
                return rand <= Prob_Green;
            }
        }
    
    }

}


namespace TestWebAPI.LLMWrapper
{
    public class JSON_PROMPT
    {
        public string? SYSTEM_PROMPT { get; set; }
        public string? USER_HEADER { get; set; }
        public string? USER_INSTRUCTIONS { get; set; }
    }
    public class VacancyQA
    {
        // class def for logging
        private readonly ILogger<VacancyQA>? _logger;
        private readonly string? _teststring;

        public VacancyQA(ILogger<VacancyQA> logger)
        {
            _logger = logger;
        }


        public VacancyQA(string teststring = "t")
        {
            _teststring = teststring;
        }

        public Dictionary<string, string> GetPrompts(string inputpath = "C:\\Users\\manthony2\\OneDrive - Department for Education\\Documents\\GitHub\\AIVacancyQualityAssurance\\data\\PromptTemplate_V0_D1.json")
        {
            string js = new(File.ReadAllText(inputpath));
            //Console.WriteLine(js);
            ArgumentNullException.ThrowIfNull(js);

            JSON_PROMPT jsondict = JsonSerializer.Deserialize<JSON_PROMPT>(js);
            //Console.WriteLine(jsondict);

            //call an empty dictionary constructor
            Dictionary<string, string> outdict = new Dictionary<string, string>();
            outdict["SYSTEM_PROMPT"] = jsondict.SYSTEM_PROMPT ?? "-";
            outdict["USER_HEADER"] = jsondict.USER_HEADER ?? "-";
            outdict["USER_INSTRUCTION"] = jsondict.USER_INSTRUCTIONS ?? "-";
            return outdict;
        }

        public bool FlagifyLLMResponse(
            // Function to convert text LLM response into boolean flags
            string LLMtext = "" // input llm text
            , bool invert_logic = false // invert boolean - relevant if you need to identify YES as a bad result
            , bool spelling_check = false
            )// we want the output to always be 1 = Test Failed, 0 = Test Passed (or null)
        {
            string textfilt = new string(LLMtext);
            string lowertext = textfilt.ToLower();


            if (spelling_check)
            {
                bool containsnone = lowertext.Contains("none");
                // spelling check is simpler - check for existence of "None" keyword as this a specific prompt directive
                if (containsnone) { return false; }
                else { return true; }
            }
            bool containsyes = lowertext.Contains("yes");
            bool containsno = lowertext.Contains("no");
            if (invert_logic)
                if (containsyes)
                { return false; } // test passes in this instance
                else
                {
                    if (containsno)
                    {
                        return true; // test fails
                    }
                    return false;
                }

            if (containsyes)
            {
                return true;
            }
            else
            {
                if (containsno)
                {
                    return false;
                }
                return false ;
            }
        }


        public string CallLLM(
            string SystemHeader = "'You are a reviewer of apprenticeship vacancies, and you must be clear, concise, professional and polite in your responses, and do not use slang, inappropriate language or emojis in any responses'"

            , string MainDirective = "Please assess the following vacancy for spelling / grammar errors, returning YES and explaining answers if you find any, or NO if you do not."
            , string AdditionalDirective = "Please review the following document:"
            , string VacancyTextToReview = "This is not a real apprenticeship vacancy for review"
            )
        {
            //function to Run the LLM code given a specific user directive
            //Console.WriteLine("HELLO FROM INSIDE THE LLM CODE");

            // ideally we want to read the JSON from a config, but this is hardcoded link to the relevant keys - we only need these keys.
            // we need a secret to store these properly, but this is a second order problem.
            var configuration = new ConfigurationBuilder().AddJsonFile("C:\\Users\\manthony2\\OneDrive - Department for Education\\Documents\\testdotnetproject\\local.settings.json", optional: false, reloadOnChange: true)
            .Build();
            string conn_key = configuration.GetSection("Values").GetValue<string>("VACANCYQA_LLM_KEY")?? "NO KEY DEFINED";
            string conn_URL = configuration.GetSection("Values").GetValue<string>("VACANCYQA_LLM_ENDPOINT_SHORT")?? "NO URL DEFINED";

            // full URL: "https://api.education.gov.uk/openai/deployments/gpt-4o/chat/completions?api-version=2024-08-01-preview"
            Uri LLMendpoint = new Uri(conn_URL);
            AzureKeyCredential azureKeyCredential = new AzureKeyCredential(new(conn_key));
            AzureOpenAIClient azureclient = new(
                 LLMendpoint,
                 azureKeyCredential
              );

            string InputDirective = string.Format("""
                {0}

                {1}

                {2}
                """, [MainDirective, AdditionalDirective, VacancyTextToReview]);


            ChatClient chatclient = azureclient.GetChatClient("gpt-4o");
            //Console.WriteLine(chatclient);
            ChatCompletion resp = chatclient.CompleteChat(
                [
                    // System messages represent instructions or other guidance about how the assistant should behave
                    new SystemChatMessage(SystemHeader),
                        // User messages represent user input, whether historical or the most recent input
                        new UserChatMessage(
                            InputDirective
                            )
                    ]
             );
            var outputstring = new string(resp.Content[0].Text);
            //Console.WriteLine("LLM OUTPUT");
            //Console.WriteLine(outputstring);
            return outputstring;
        }
    }
}


namespace TestWebAPI.LLMExecutable
{
    class LLMExec
    {
        public LLMExec() { } // class constructor - doesn't do anything
        public string ExecLLM(InputObject Vacancy_input) // simple LLM output returns a battery of tests
        {
            //apply method to get the vacancy from the spec
            Vacancy_input.Create_VacancyText();
          
            /*
            //temp object for vacancy
            Dictionary<string, string> Vacancy = new Dictionary<string, string>();

            Vacancy.Add("VacancyId", "VAC10234567");
            Vacancy.Add("VacancySnapshot.Title", "Junior Software Developer");
            Vacancy.Add("VacancySnapshot.ShortDescription", "Join our dynamic tech team as a Junior Software Developer, where you’ll contribute to building innovative applications and gain han3ds-on experience in modern development practices.");
            Vacancy.Add("VacancySnapshot.Description", "We are seeking a motivated Junior Softwdare Developer to assist in designing, coding, and testing software solutions. You will work cl2osely wi th senior developers and project managers to deliver high-quality applications that meet client requirements. This role offers an excellent opportunity to grow your technical skills and advance your career in software development.");
            Vacancy.Add("VacancySnapshot.EmployerDescription", "TechNova Solutions is a forwtard-thinking technology company specializing in custom software development, cloud solutio ns, and digital transformation. Our mission is to empower businesses through cutting-edge technology and exceptional service.");
            Vacancy.Add("VacancySnapshot.Skills", "Proficiency in at least one programming language (e.g., Python, Java, C#)\r\nBasic understanding of web technologieps (HTML, CSS, JavaScript)\r\nProblem-solving and analytical thinking\r\nAbility to work collaboratively in a team environment\r\nStrong communication skills");
            Vacancy.Add("VacancySnapshot.Qualifications", "Bachelor’s degree in Computer Science, Software Engineerping, or related field (or equivalent experience)\r\nFamiliarity with version control systems (e.g., Git)\r\nKnowledge of software development lifecycle (SDLC)");
            Vacancy.Add("VacancySnapshot.ThingsToConsider", "This is an entry-level position with opportunities for growth\r\nHybrid work model (2 days in office, 3 days remote)\r\nOccasional overtime during project deadlines\r\nMust be eligible to work in the country");
            Vacancy.Add("VacancySnapshot.TrainingDescription", "Comprehensive onboarding program covering company tools, coding standards, and agile methodologies. You will receive mentorship 5from senior developers and participate in weekly knowledge-sharing sessions.");
            Vacancy.Add("VacancySnapshot.AdditionalTrainingDescription", "Access to online learning platforms for certifications in cloud technologies, advanced programming, and software archi4tecture. Opportunities to attend industry conferences and workshops.");
            Vacancy.Add("Vacancy_Full", "\r\nTitle\r\nJunior Software Developer\r\n\r\nShort Description\r\nJoin our dynamic tech team as a Junior Sofware Devloper, where you’ll contribute to building innovative aipplications and gain hands-on experiencye in modern development pra ctices.\r\n\r\nDescription\r\nWe are seeking a motivated Junior Software Developer to assist in designing, coding, and testing software solutions. You will work closely with senior developers and project managers to deliver high-quality applications that meet client requirements. This role offers an excellent opportunity to grow your technical skills and advance your career in software development.\r\n\r\nEmployer Description\r\nTechNova Solutions is a forward-thinking technology company specializing in custom software development, cloud solutions, and digital transformation. Our mission is to empower businesses through cutting-edge technology and exceptional service.\r\n\r\nSkills\r\n\r\nProficiency in at least one programming language (e.g., Python, Java, C#)\r\nBasic understanding of web technologies (HTML, CSS, JavaScript)\r\nProblem-solving and analytical thinking\r\nAbility to work collaboratively in a team environment\r\nStrong communication skills\r\n\r\n\r\nQualifications\r\n\r\n 18+\r\r\nBachelor’s degree in Computer Science, Software Engineering, or related field (or equivalent experience)\r\nFamiliarity with version control systems (e.g., Git)\r\nKnowledge of software development lifecycle (SDLC)\r\n\r\n\r\nThings to Consider\r\n\r\nThis is an entry-level position with opportunities for growth\r\nHybrid work model (2 days in office, 3 days remote)\r\nOccasional overtime during project deadlines\r\nMust be eligible to work in the country\r\n\r\n\r\nTraining Description\r\nComprehensive onboarding program covering company tools, coding standards, and agile methodologies. You will receive mentorship from senior developers and participate in weekly knowledge-sharing sessions.\r\n\r\nAdditional Training Description\r\nAccess to online learning platforms for certifications in cloud technologies, advanced programming, and software architecture. Opportunities to attend industry conferences and workshops.");
            Vacancy.Add("Vacancy_NoSkills", "\r\nTitle\r\nJunior Software Developer\r\n\r\nShort Description\r\nJoin our dynamic tech team as a Junior Software Developer, where you’ll contribute to building innovative applications and gain hands-on experience in modern development practices.\r\n\r\nDescription\r\nWe are seeking a motivated Junior Software Developer to assist in designing, coding, and testing software solutions. You will work closely with senior developers and project managers to deliver high-quality applications that meet client requirements. This role offers an excellent opportunity to grow your technical skills and advance your career in software development.\r\n\r\nEmployer Description\r\nTechNova Solutions is a forward-thinking technology company specializing in custom software development, cloud solutions, and digital transformation. Our mission is to empower businesses through cutting-edge technology and exceptional service.\r\n\r\nSkills\r\n\r\nProficiency in at least one programming language (e.g., Python, Java, C#)\r\nBasic understanding of web technologies (HTML, CSS, JavaScript)\r\nProblem-solving and analytical thinking\r\nAbility to work collaboratively in a team environment\r\nStrong communication skills\r\n\r\n\r\nQualifications\r\n\r\nBachelor’s degree in Computer Science, Software Engineering, or related field (or equivalent experience)\r\nFamiliarity with version control systems (e.g., Git)\r\nKnowledge of software development lifecycle (SDLC)\r\n\r\n\r\nThings to Consider\r\n\r\nThis is an entry-level position with opportunities for growth\r\nHybrid work model (2 days in office, 3 days remote)\r\nOccasional overtime during project deadlines\r\nMust be eligible to work in the country\r\n\r\n\r\nTraining Description\r\nComprehensive onboarding program covering company tools, coding standards, and agile methodologies. You will receive mentorship from senior developers and participate in weekly knowledge-sharing sessions.\r\n\r\nAdditional Training Description\r\nAccess to online learning platforms for certifications in cloud technologies, advanced programming, and software architecture. Opportunities to attend industry conferences and workshops.");

            string json_serialized_input = JsonSerializer.Serialize(Vacancy);
            Console.WriteLine("INPUT JSON:");
            Console.WriteLine(json_serialized_input);

            InputObject Vacancy_input = JsonSerializer.Deserialize<InputObject>(json_serialized_input);
            */
            Console.WriteLine(Vacancy_input.ToString());
            

            Console.WriteLine("Hello, World!");
            //call class constructor for the VacancyQA class - call it without a logger method being passed in
            Console.WriteLine("Now running LLM code");
            LLMWrapper.VacancyQA qa = new("Testlogger");
            Dictionary<string, string> llmprompt_discrim = qa.GetPrompts("C:\\Users\\manthony2\\OneDrive - Department for Education\\Documents\\GitHub\\AIVacancyQualityAssurance\\data\\PromptTemplate_V0_D1.json");
            Dictionary<string, string> llmprompt_missingcontent = qa.GetPrompts("C:\\Users\\manthony2\\OneDrive - Department for Education\\Documents\\GitHub\\AIVacancyQualityAssurance\\data\\PromptTemplate_TextConsistency.json");
            Dictionary<string, string> llmprompt_spellingcheck = qa.GetPrompts("C:\\Users\\manthony2\\OneDrive - Department for Education\\Documents\\GitHub\\AIVacancyQualityAssurance\\data\\PromptTemplate_SpellingAndGrammar.json");
            

            Console.WriteLine("Discrimination check");
            string llmoutputcheck_discrimination = qa.CallLLM(
                llmprompt_discrim["SYSTEM_PROMPT"],
                llmprompt_discrim["USER_HEADER"],
                llmprompt_discrim["USER_INSTRUCTION"],
                Vacancy_input.Vacancy_full??" "
                );
            Console.WriteLine("Text Inconsistency/ Missing content check");
            string llmoutputcheck_missingcontent = qa.CallLLM(
                llmprompt_missingcontent["SYSTEM_PROMPT"],
                llmprompt_missingcontent["USER_HEADER"],
                llmprompt_missingcontent["USER_INSTRUCTION"],
                Vacancy_input.Vacancy_full??""
            );

            bool status_code_discrim = qa.FlagifyLLMResponse(llmoutputcheck_discrimination, false, false);
            bool status_code_missingcontent = qa.FlagifyLLMResponse(llmoutputcheck_missingcontent, true, false);

            Console.WriteLine(string.Format("DISCRIMINATION CHECK RESULT: {0}", [status_code_discrim]));
            if (status_code_discrim) {
                Console.WriteLine(string.Format("DETAIL: {0} \n\n\n\n", [llmoutputcheck_discrimination]));
            }
            Console.WriteLine(string.Format("TEXT INCONSISTENCY CHECK RESULT: {0}", [status_code_missingcontent]));
            if (status_code_missingcontent)
            {
                Console.WriteLine(string.Format("DETAIL: {0} \n\n\n\n", [llmoutputcheck_missingcontent]));
            }

            //now we've got the codes, lets collect them in an object.

            AICheckOutput discrimcheck = new(status_code_discrim, llmoutputcheck_discrimination, "DiscriminationCheck");
            AICheckOutput textinconsistencycheck = new(status_code_missingcontent, llmoutputcheck_missingcontent, "TextInconsistencyCheck");
            List<AICheckOutput> aichecks_shortlist = [discrimcheck, textinconsistencycheck];

            // Spelling & Grammar check(s)
            Console.WriteLine("Spelling & Grammar check");
            

            //create set of spelling check
            SpellingChecks spellingChecks = new SpellingChecks();
            spellingChecks.Checks = new List<AICheckOutput>();

            Dictionary<string, string> spagcheckdict=new Dictionary<string, string>();
            spagcheckdict.Add("Description", Vacancy_input.Description??"=");
            spagcheckdict.Add("ShortDescription", Vacancy_input.Short_description??"");
            spagcheckdict.Add("Qualifications", Vacancy_input.Qualifications??"-");
            spagcheckdict.Add("Skills", Vacancy_input.Skills??"-");
            spagcheckdict.Add("Title",Vacancy_input.Title??"-");
            spagcheckdict.Add("EmployerDescription", Vacancy_input.Employer_description??"-");
            spagcheckdict.Add("TrainingDesiption", Vacancy_input.Training_description??"-");
            spagcheckdict.Add("AdditionalTrainingDescription", Vacancy_input.Additional_training_description??"-");


            List<string> listofkeys = new List<string>(spagcheckdict.Keys);





            foreach (string key in listofkeys) {                
                    Console.WriteLine("SPELLING GRAMMAR CHECK FOR ");
                    Console.WriteLine(key);
                    
                    string llmoutputcheck_spelling = qa.CallLLM(
                    llmprompt_spellingcheck["SYSTEM_PROMPT"],
                    llmprompt_spellingcheck["USER_HEADER"],
                    llmprompt_spellingcheck["USER_INSTRUCTION"],
                    spagcheckdict[key]
                    );
                    bool status_code_spellinggramar_1 = qa.FlagifyLLMResponse(llmoutputcheck_spelling, false, true);
                    Console.WriteLine(string.Format("Spelling check Failure result for : {0} = {1}", [key, status_code_spellinggramar_1]));
                    if (status_code_spellinggramar_1) {
                        Console.WriteLine(string.Format("Detailed Result : {0} \n", [llmoutputcheck_spelling]));
                    }
                    AICheckOutput spag_check = new(status_code_spellinggramar_1,llmoutputcheck_spelling,string.Format("Spelling Check {0}", [key]));
                    spellingChecks.Checks.Add(spag_check);                
            }

            bool status_code_spellinggramar = (spellingChecks.EvaluateAllSpellingChecks()).Value;
            aichecks_shortlist.Add(spellingChecks.EvaluateAllSpellingChecks());
            List<AICheckOutput> aiChecks_debug = aichecks_shortlist.Concat(spellingChecks.Checks).ToList();

            string retline = string.Format("Discrimination flag (Failure=1): {0}, Text consistency (Failure = 1): {1}, Spelling Check (Failure =1): {2}", [status_code_discrim, status_code_missingcontent, status_code_spellinggramar]);
            Console.WriteLine("StatusCodes");
            Console.WriteLine(retline);


            // initialize the traffic light system & Allocation system
            PrioritisationSystem priosystem = new();
            ReviewAllocator alloc = new();

            TrafficLight traf= priosystem.TrafficLightAssignment(aichecks_shortlist);

            bool alloc_review = alloc.Allocator(traf);

            AICheckReturnResultObject ReturnObject = new AICheckReturnResultObject
            {
                DebugAICheckOutput = aiChecks_debug,
                AICheckOutput = aichecks_shortlist,
                VacancyID = Vacancy_input.VacancyId ?? "-"
                ,
                TrafficLightScore = traf
                ,
                RecommendReview = alloc_review
            };

            Console.WriteLine(ReturnObject);
            JsonSerializerOptions jopts= new JsonSerializerOptions { WriteIndented = true };
            string jsonstring = JsonSerializer.Serialize(ReturnObject,jopts);
            Console.WriteLine(jsonstring);
            return jsonstring;
        }

    }
}