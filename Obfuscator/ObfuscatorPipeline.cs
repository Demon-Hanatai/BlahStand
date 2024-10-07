public class ObfuscatorPipeline
{
    private NameObfuscator nameObfuscator = new NameObfuscator();
    private ControlFlowObfuscator controlFlowObfuscator = new ControlFlowObfuscator();
    private RandomCodeInjector randomCodeInjector = new RandomCodeInjector();

        public string Obfuscate(string code)
    {
                code = nameObfuscator.ObfuscateNames(code);

                code = controlFlowObfuscator.ObfuscateControlFlow(code);

                code = randomCodeInjector.InjectRandomCode(code);

        return code;
    }
}
