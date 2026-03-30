import React, { useState } from 'react';
import { Shield, Lock, User, AlertCircle, Loader2 } from 'lucide-react';
import './index.css';

function App() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isProcessing = React.useRef(false);

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    if (isBusy || isProcessing.current) return;

    isProcessing.current = true;
    setIsBusy(true);
    setError(null);

    // Simulate system delay
    setTimeout(() => {
      if (username === 'admin' && password === '12345') {
        alert('Login Successful! Welcome to the Digital Twin.');
      } else {
        setError('بيانات الدخول غير صحيحة. يرجى التأكد من اسم المستخدم وكلمة المرور.');
      }
      setIsBusy(false);
      isProcessing.current = false;
    }, 1500);
  };

  return (
    <div className="animate-fade-in flex w-full max-w-6xl h-[600px] shadow-2xl overflow-hidden rounded-[32px] border border-white/10">
      {/* Left Side: Branding */}
      <div className="hidden md:flex w-1/2 bg-gradient-to-br from-[#0F172A] to-[#1E293B] flex-col justify-center items-center p-12 relative overflow-hidden">
        <div className="absolute top-0 left-0 w-full h-full opacity-10 pointer-events-none">
          <div className="absolute top-[-10%] right-[-10%] w-[300px] h-[300px] bg-amber-500 blur-[120px] rounded-full"></div>
          <div className="absolute bottom-[-10%] left-[-10%] w-[200px] h-[200px] bg-blue-500 blur-[100px] rounded-full"></div>
        </div>

        <img src="/logo.png" alt="System Logo" className="w-64 h-64 object-contain mb-8 filter drop-shadow(0 0 20px rgba(217, 119, 6, 0.4))" />
        <h1 className="text-4xl font-bold text-white text-center mb-4 tracking-tight">التوأم الرقمي</h1>
        <p className="text-slate-400 text-center text-lg max-w-sm">نظام محاكاة متطور لاختبار صمود المنظومة وجودتها التقنية.</p>
      </div>

      {/* Right Side: Login Form */}
      <div className="w-full md:w-1/2 bg-slate-900 flex flex-col justify-center p-12">
        <div className="max-w-sm mx-auto w-full">
          <div className="flex items-center gap-3 mb-8">
            <div className="p-3 glass-card rounded-xl">
              <Shield className="text-amber-500" size={32} />
            </div>
            <div>
              <h2 className="text-2xl font-bold text-white">تسجيل الدخول</h2>
              <p className="text-slate-400 text-sm">أدخل بياناتك للبدء بالمحاكاة</p>
            </div>
          </div>

          <form onSubmit={handleLogin} className="space-y-6">
            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-300 block">اسم المستخدم</label>
              <div className="relative">
                <User className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500" size={20} />
                <input
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="input-field pl-12"
                  placeholder="أدخل اسم المستخدم"
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-300 block">كلمة المرور</label>
              <div className="relative">
                <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500" size={20} />
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="input-field pl-12"
                  placeholder="••••••••"
                  required
                />
              </div>
            </div>

            {error && (
              <div className="p-4 bg-red-500/10 border border-red-500/20 rounded-xl flex items-center gap-3 text-red-400 text-sm">
                <AlertCircle size={18} />
                <span>{error}</span>
              </div>
            )}

            <button
              type="submit"
              disabled={isBusy}
              className="btn-primary w-full flex justify-center items-center gap-2"
            >
              {isBusy ? <Loader2 className="animate-spin" size={20} /> : 'دخول'}
            </button>
          </form>

          <div className="mt-12 pt-8 border-t border-white/5 text-center">
            <p className="text-slate-500 text-xs">Playwright Simulation Environment v1.0</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;
