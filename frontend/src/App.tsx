import { useState } from 'react'
import './App.css'
import axios from 'axios'

interface IDoc {
  key: string
  name: string
  url: string
}
const MockDocs: IDoc[] = [
  { key: "401K_Policy.pdf", name: "401K Policy", url: "https://www.contoso.com/Contoso-Company_Benefits.pdf" },
  { key: "Company_Benefits.pdf", name: "Company Benefits", url: "https://www.contoso.com/Contoso-Company_Benefits.pdf" },
  { key: "Medical_Leave_Policy.pdf", name: "Medical Leave", url: "https://www.contoso.com/Contoso-Company_Benefits.pdf" },
]

interface ICompletionResponse {
  query: string
  text: string
  usage: IUsage
  learnMore: ILearnMore[];
  deepText: string
}

interface IUsage {
  completionTokens: number
  promptTokens: number
  totalTokens: number
}

interface ILearnMore {
  collection: string
  key: string;
  name: string;
}
interface ISettings {
  chunkSize: string
  maxTokens: string
  temperature: string
  limit: string
  minRelevanceScore: string
}

const DefaultSettings = {
  chunkSize: "500",
  maxTokens: "2000",
  temperature: "0.3",
  limit: "3",
  minRelevanceScore: "0.7"
}

function App() {
  // State
  const [query, setQuery] = useState<string>("")
  const [docs] = useState<IDoc[]>(MockDocs)
  const [stype, setStype] = useState<string>("RAG")
  const [conversation, setConversation] = useState<ICompletionResponse[]>([])
  const [selectedDoc, setSelectedDoc] = useState<string>("")
  const [setting, setSettings] = useState<ISettings>(DefaultSettings);
  const [deepText, setDeepText] = useState<string>("")

  const CleanKey = (key: string) =>
    key.replace(/-/g, " ").replace(/_/g, " ").substring(0, key.lastIndexOf("."))

  const ResetSearch = () => {
    setStype("RAG")
  }

  const Query = async () => {
    const payload = {
      query,
      maxTokens: parseInt(setting.maxTokens),
      limit: parseInt(setting.limit),
      minRelevanceScore: parseFloat(setting.minRelevanceScore)
    }

    try {
      const resp = await axios.post<ICompletionResponse>("http://localhost:5137/api/gpt/query", payload);
      const data = resp.data
      setConversation([...conversation, data])
    } catch (err) {
      console.log(err)
    }
  }

  const DeepQuery = async () => {
    const payload = {
      key: selectedDoc,
      prompt: "Summarize in two sentences. " + query,
      chunk_size: parseInt(setting.chunkSize),
      max_tokens: parseInt(setting.maxTokens)
    }
    try {
      const resp = await axios.post<ICompletionResponse>("http://localhost:5137/api/gpt/summarize", payload);
      const data = resp.data
      setDeepText(data.deepText)
    } catch (err) {
      console.log(err);
    }
  }

  return (
    <>
      <nav className='p-2 bg-blue-900 text-white font-bold'>Enhanced RAG Pattern with Summarizer</nav>
      <div className='hidden p-2 bg-blue-800 text-white flex flex-row space-x-2'>
        {/* search method */}
        <label>Search:</label>
        <input type='radio' id='stype1' name='stype' value={"RAG"}
          onChange={(e) => setStype(e.target.value)}
          checked={stype === "RAG"}
        >
        </input>
        <label>Across documents</label>
        <input type='radio' id='stype2' name='stype' value={"RAGS"}
          onChange={(e) => setStype(e.target.value)}
          checked={stype === "RAGS"}
        ></input>
        <label>Single document</label>
        <button
          onClick={ResetSearch}
          className='p-1 bg-orange-700 text-sm font-semibold rounded'>Reset</button>
      </div>
      <div className='p-2 bg-blue-700 text-white flex flex-row flex-wrap space-x-2 items-center'>
        <div className='space-x-2'>
          <label>Chunk Size:</label>
          <input type='text' className='w-20 px-1 rounded text-black'
            onChange={(e) => setSettings({ ...setting, chunkSize: e.target.value })}
            value={setting.chunkSize}
          />
        </div>
        <div className='space-x-2'>
          <label>Max Tokens:</label>
          <input type='text' className='w-20 px-1 rounded text-black'
            onChange={(e) => setSettings({ ...setting, maxTokens: e.target.value })}
            value={setting.maxTokens}
          />
        </div>
        <div className='space-x-2'>
          <label>Temperature:</label>
          <input type='text' className='w-20 px-1 rounded text-black'
            onChange={(e) => setSettings({ ...setting, temperature: e.target.value })}
            value={setting.temperature}
          />
        </div>
        <div className='space-x-2'>
          <label>Limit:</label>
          <input type='text' className='w-20 px-1 rounded text-black'
            onChange={(e) => setSettings({ ...setting, limit: e.target.value })}
            value={setting.limit}
          />
        </div>
        <div className='space-x-2'>
          <label>Relevance:</label>
          <input type='text' className='w-20 px-1 rounded text-black'
            onChange={(e) => setSettings({ ...setting, minRelevanceScore: e.target.value })}
            value={setting.minRelevanceScore}
          />
        </div>
      </div>
      <main>
        <div className="flex flex-row">
          {/* side panel and main */}
          <div className="w-1/4 p-3 bg-blue-100 flex flex-col space-y-2">
            {/* side panel */}
            <label className='font-semibold'>Available Documents</label>
            {docs.map((doc, idx) => <button className='p-1 bg-blue-700 text-white border rounded hover:border-blue-950 hover:bg-blue-600' key={idx}>{doc.name}</button>)}
          </div>

          <div className="w-3/4 p-2 flex flex-col space-y-2">
            {/* main area */}
            <textarea className='w-full border rounded-md bg-slate-50 p-1' rows={4} placeholder='What is your question?'
              onChange={(e) => setQuery(e.target.value)}
              value={query}
            ></textarea>
            <button className='p-1 bg-orange-700 w-32 text-white rounded hover:border-orange-950 hover:bg-orange-600'
              onClick={Query}>Submit</button>
            {conversation.map((msg, idx) => <div key={idx} className='border flex flex-col w-full space-y-2'>
              <div className='p-2 bg-blue-100 w-3/4'>{msg.query}</div>
              <div className=' ml-auto bg-blue-200 w-3/4 flex flex-col'>
                <div className="p-2 w-full">
                  <p>{msg.text}</p>
                </div>
                <hr />
                <div className="bg-blue-300 p-2 w-full flex flex-col">
                  <label className='text-sm uppercase font-semibold'>Learn More: (Select a document for deep summarization insights)</label>
                  <div className="flex flex-row space-x-2 justify-center mt-2 mb-2">
                    {msg.learnMore && msg.learnMore.map((doc, idx) =>
                      <>
                        <div className='space-x-2'>
                          <input type='radio' name='doc' value={doc.key}
                            onChange={(e) => setSelectedDoc(e.target.value)}
                          />
                          <label className='p-1 bg-blue-700 text-white rounded hover:border-blue-950 hover:bg-blue-600' key={idx}>
                            {doc.name}
                          </label>
                        </div>
                      </>
                    )}

                  </div>
                  <hr className={'' + (selectedDoc ? "visible" : "hidden")} />
                  <div className={"flex flex-col space-y-2 mt-2 " + (selectedDoc ? "visible" : "hidden")}>
                    <button className={"p-1 w-32 bg-orange-700 text-white rounded hover:border-orange-950 hover:bg-orange-600 "}
                      onClick={DeepQuery}
                    >Deep Insights</button>
                    <hr className='border-slate-300' />
                    <div className="flex flex-row">
                      <p>{deepText}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>)}
          </div>
        </div>
      </main>
    </>
  )
}

export default App
