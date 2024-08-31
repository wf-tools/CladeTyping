-- このツールは何？
このツールはNextcladeで公開されている型判定ツールをWindows上で簡単に利用するために作られました。
https://docs.nextstrain.org/projects/nextclade/en/stable/

-- 何ができるの？
NGSシーケンサ（Illumina pairend）の出力FastqをDorag＆DropするだけでNextclade CLI の結果が出力されます。

-- 具体的に？
・入力されたFastqのペアエンドを判定します。
・Fastpにより、入力されたFastqファイルの情報とQCを行います。　https://github.com/OpenGene/fastp　（MIT license）
・ペアエンドのマッピングを行います。マッピングツールは3種類用意しています。
　　--- BWA mem https://github.com/lh3/bwa　（GPL-3.0 license）
　　--- Bowtie2 https://github.com/BenLangmead/bowtie2　（GPL-3.0 license）
　　--- minimap2 https://github.com/lh3/minimap2　（MIT License）
・samtool にて samからbamに変換 https://github.com/samtools/samtools （MIT/Expat License）
・ConsensusFixerによりマッピング結果からコンセンサス配列を作成します。https://github.com/cbg-ethz/ConsensusFixer（GPL-3.0 license）
・Nextclade CLI で型判定を行います。 https://github.com/nextstrain/nextclade （MIT license）

そのほか、リファレンス配列とコンセンサス配列のアライメントを見るため AliView があります。https://github.com/AliView/AliView （GPL-3.0 license）

